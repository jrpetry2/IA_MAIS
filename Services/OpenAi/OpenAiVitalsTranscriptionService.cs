using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaphMovel.Configuration;
using SaphMovel.Services.SinaisVitais;

namespace SaphMovel.Services.OpenAi;

/// <summary>Áudio → transcrição → chat (extrator clínico) → JSON de sinais vitais.</summary>
public sealed class OpenAiVitalsTranscriptionService : IOpenAiVitalsTranscriptionService
{
	const string TranscriptionsUrl = "https://api.openai.com/v1/audio/transcriptions";
	const string ChatCompletionsUrl = "https://api.openai.com/v1/chat/completions";

	readonly HttpClient _http;
	readonly OpenAiTranscriptionOptions _options;
	readonly ILogger<OpenAiVitalsTranscriptionService> _log;

	public OpenAiVitalsTranscriptionService(
		HttpClient http,
		IOptions<OpenAiTranscriptionOptions> options,
		ILogger<OpenAiVitalsTranscriptionService> log)
	{
		_http = http;
		_options = options.Value;
		_log = log;
	}

	public async Task<OpenAiVitalsTranscriptionResult> TranscribeVitalsJsonAsync(
		string audioFilePath,
		CancellationToken cancellationToken = default)
	{
		if (!TryResolveApiKey(out var apiKey, out var keyError))
			return OpenAiVitalsTranscriptionResult.Fail(keyError);

		if (!File.Exists(audioFilePath))
			return OpenAiVitalsTranscriptionResult.Fail("Ficheiro de áudio não encontrado.");

		if (!await WaitForNonEmptyFileAsync(audioFilePath, cancellationToken).ConfigureAwait(false))
			return OpenAiVitalsTranscriptionResult.Fail("Gravação vazia ou ainda não guardada. Tente gravar de novo.");

		byte[] audioBytes;
		try
		{
			audioBytes = await File.ReadAllBytesAsync(audioFilePath, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "OpenAI: leitura de {Path}", audioFilePath);
			return OpenAiVitalsTranscriptionResult.Fail("Não foi possível ler o ficheiro de áudio.");
		}

		if (audioBytes.Length == 0)
			return OpenAiVitalsTranscriptionResult.Fail("Ficheiro de áudio vazio.");

		try
		{
			var transcriptionBody = await PostAudioTranscriptionAsync(
				apiKey,
				audioBytes,
				audioFilePath,
				cancellationToken).ConfigureAwait(false);

			if (transcriptionBody is null)
				return OpenAiVitalsTranscriptionResult.Fail("Transcrição de áudio falhou.");

			if (!TryExtractTranscriptionText(transcriptionBody, out var transcribedText))
				return OpenAiVitalsTranscriptionResult.Fail(
					$"Transcrição sem texto utilizável. Trecho: {Truncate(transcriptionBody, 200)}");

			_log.LogDebug("OpenAI transcrição (texto): {Text}", Truncate(transcribedText, 400));

			var vitalsJson = await ExtractVitalsJsonFromTextAsync(
				apiKey,
				transcribedText,
				cancellationToken).ConfigureAwait(false);

			if (vitalsJson is null)
				return OpenAiVitalsTranscriptionResult.Fail(
					"Extração de sinais vitais falhou (resposta sem JSON PAS/PAD/FC).");

			return OpenAiVitalsTranscriptionResult.Ok(vitalsJson);
		}
		catch (OperationCanceledException)
		{
			return OpenAiVitalsTranscriptionResult.Fail("Transcrição cancelada.");
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "OpenAI: erro inesperado.");
			return OpenAiVitalsTranscriptionResult.Fail($"Erro na transcrição: {ex.Message}");
		}
	}

	async Task<string?> PostAudioTranscriptionAsync(
		string apiKey,
		byte[] audioBytes,
		string audioFilePath,
		CancellationToken cancellationToken)
	{
		using var form = BuildMultipartForm(audioBytes, audioFilePath);
		using var request = new HttpRequestMessage(HttpMethod.Post, TranscriptionsUrl) { Content = form };
		request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

		_log.LogInformation(
			"OpenAI transcrição (áudio): {Bytes} bytes, model={Model}",
			audioBytes.Length,
			_options.Model);

		using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			_log.LogWarning(
				"OpenAI transcrição HTTP {Status}: {Body}",
				(int)response.StatusCode,
				Truncate(body, 800));
			return null;
		}

		_log.LogDebug("OpenAI transcrição (resposta): {Body}", Truncate(body, 500));
		return body;
	}

	async Task<string?> ExtractVitalsJsonFromTextAsync(
		string apiKey,
		string transcribedText,
		CancellationToken cancellationToken)
	{
		using var request = BuildChatExtractionRequest(apiKey, transcribedText);
		_log.LogInformation("OpenAI extração clínica: model={Model}", _options.ChatModel);

		using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			_log.LogWarning(
				"OpenAI extração HTTP {Status}: {Body}",
				(int)response.StatusCode,
				Truncate(body, 800));
			return null;
		}

		_log.LogDebug("OpenAI extração (resposta): {Body}", Truncate(body, 500));

		if (!TryExtractChatAssistantContent(body, out var assistantContent))
			return null;

		return SinaisVitaisTranscriptionJsonExtractor.TryExtract(assistantContent, out var vitalsJson)
			? vitalsJson
			: null;
	}

	HttpRequestMessage BuildChatExtractionRequest(string apiKey, string transcribedText)
	{
		var payload = new
		{
			model = _options.ChatModel,
			temperature = 0,
			response_format = new { type = "json_object" },
			messages = new object[]
			{
				new { role = "system", content = OpenAiTranscriptionOptions.VitalsExtractionSystemPrompt },
				new { role = "user", content = transcribedText },
			},
		};

		var json = JsonSerializer.Serialize(payload);
		var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json"),
		};
		request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
		return request;
	}

	static bool TryExtractTranscriptionText(string body, out string text)
	{
		text = "";
		try
		{
			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			if (root.TryGetProperty("text", out var textEl))
			{
				text = textEl.GetString()?.Trim() ?? "";
				return text.Length > 0;
			}
		}
		catch (JsonException)
		{
			/* corpo pode ser texto simples */
		}

		text = body.Trim();
		return text.Length > 0;
	}

	static bool TryExtractChatAssistantContent(string body, out string content)
	{
		content = "";
		try
		{
			using var doc = JsonDocument.Parse(body);
			if (!doc.RootElement.TryGetProperty("choices", out var choices)
				|| choices.ValueKind != JsonValueKind.Array
				|| choices.GetArrayLength() == 0)
				return false;

			var message = choices[0].GetProperty("message");
			content = message.GetProperty("content").GetString()?.Trim() ?? "";
			return content.Length > 0;
		}
		catch
		{
			return false;
		}
	}

	MultipartFormDataContent BuildMultipartForm(byte[] audioBytes, string audioFilePath)
	{
		var form = new MultipartFormDataContent();
		var (fileName, mime) = ResolveUploadFileNameAndMime(audioBytes, audioFilePath);

		var fileContent = new ByteArrayContent(audioBytes);
		fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mime);
		form.Add(fileContent, "file", fileName);

		form.Add(new StringContent(_options.Model, Encoding.UTF8), "model");
		form.Add(new StringContent(_options.Language, Encoding.UTF8), "language");
		form.Add(new StringContent(OpenAiTranscriptionOptions.VitalsTranscriptionPrompt, Encoding.UTF8), "prompt");
		form.Add(new StringContent(_options.ResponseFormat, Encoding.UTF8), "response_format");
		form.Add(
			new StringContent(
				_options.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture),
				Encoding.UTF8),
			"temperature");

		return form;
	}

	static (string FileName, string Mime) ResolveUploadFileNameAndMime(byte[] bytes, string path)
	{
		if (bytes.Length >= 8
			&& bytes[4] == (byte)'f'
			&& bytes[5] == (byte)'t'
			&& bytes[6] == (byte)'y'
			&& bytes[7] == (byte)'p')
			return ("audio.m4a", "audio/mp4");

		if (bytes.Length >= 2 && bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0)
			return ("audio.mp3", "audio/mpeg");

		var ext = Path.GetExtension(path);
		if (ext.Equals(".m4a", StringComparison.OrdinalIgnoreCase)
			|| ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
			return ("audio.m4a", "audio/mp4");

		if (ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
			return ("audio.mp3", "audio/mpeg");

		return ("audio.m4a", "audio/mp4");
	}

	static async Task<bool> WaitForNonEmptyFileAsync(string path, CancellationToken ct)
	{
		long previous = -1;
		for (var i = 0; i < 12; i++)
		{
			ct.ThrowIfCancellationRequested();
			var len = new FileInfo(path).Length;
			if (len > 0 && len == previous)
				return true;
			previous = len;
			await Task.Delay(50, ct).ConfigureAwait(false);
		}

		return new FileInfo(path).Length > 0;
	}

	static string FormatHttpError(System.Net.HttpStatusCode status, string body)
	{
		var snippet = Truncate(TryParseOpenAiErrorMessage(body) ?? body, 280);
		return string.IsNullOrWhiteSpace(snippet)
			? $"Pedido falhou ({(int)status})."
			: $"Pedido falhou ({(int)status}): {snippet}";
	}

	static string? TryParseOpenAiErrorMessage(string body)
	{
		try
		{
			using var doc = JsonDocument.Parse(body);
			if (doc.RootElement.TryGetProperty("error", out var err)
				&& err.TryGetProperty("message", out var msg))
				return msg.GetString();
		}
		catch
		{
			/* ignore */
		}

		return null;
	}

	bool TryResolveApiKey(out string apiKey, out string error)
	{
		apiKey = "";
		error = "";

		var raw = _options.ApiKey?.Trim() ?? "";
		if (raw.Length == 0)
		{
			error = "Chave OpenAI não configurada.";
			return false;
		}

		if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
			raw = raw["Bearer ".Length..].Trim();

		if (raw.Length == 0)
		{
			error = "Chave OpenAI inválida.";
			return false;
		}

		apiKey = raw;
		return true;
	}

	static string Truncate(string? s, int max) =>
		string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s[..max] + "…";
}
