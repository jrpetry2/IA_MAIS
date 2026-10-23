using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;
using SaphMovel.Services.Audio;
using SaphMovel.Services.OpenAi;
using SaphMovel.Services.SinaisVitais;

namespace SaphMovel.ViewModels;

public sealed class SinaisVitaisViewModel : ObservableModel
{
	readonly MainViewModel _main;
	readonly Page _page;
	readonly IPressHoldAudioRecordService _recorder;
	readonly IOpenAiTranscriptionService _transcription;
	bool _isOuvirRecording;
	bool _isTranscribing;
	int _transcriptionGeneration;

	public SinaisVitaisFormData Form => _main.SinaisVitais;

	public string PatientEmoji => _main.PatientEmoji;
	public string PatientSummary => _main.PatientSummary;

	public bool IsOuvirRecording => _isOuvirRecording;

	public string OuvirHintText => _isTranscribing
		? "A processar áudio e preencher sinais vitais…"
		: "A gravar — mantenha pressionado. Solte para terminar.";

	public bool IsTranscribing => _isTranscribing;

	public ICommand VoltarCommand { get; }
	public ICommand EnviarCommand { get; }

	public SinaisVitaisViewModel(
		MainViewModel main,
		Page page,
		IPressHoldAudioRecordService recorder,
		IOpenAiTranscriptionService transcription)
	{
		_main = main;
		_page = page;
		_recorder = recorder;
		_transcription = transcription;
		VoltarCommand = new Command(async () => await VoltarAsync());
		EnviarCommand = new Command(async () => await EnviarAsync());
	}

	int _ouvirPressGeneration;

	public void OnOuvirPressed()
	{
		if (_isTranscribing)
			return;

		var gen = Interlocked.Increment(ref _ouvirPressGeneration);
		if (_isOuvirRecording || _recorder.IsRecording)
			return;

		SetOuvirRecording(true);
		_ = Task.Run(() => StartRecordingAsync(gen));
	}

	async Task StartRecordingAsync(int pressGeneration)
	{
		try
		{
			var ok = await _recorder.StartAsync().ConfigureAwait(false);
			if (pressGeneration != _ouvirPressGeneration)
				return;

			if (ok)
				return;

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (pressGeneration != _ouvirPressGeneration)
					return;
				SetOuvirRecording(false);
				await _page.DisplayAlertAsync(
					"Sinais Vitais",
					"Não foi possível gravar. Conceda permissão de microfone nas definições da app.",
					"OK");
			});
		}
		catch (Exception)
		{
			if (pressGeneration == _ouvirPressGeneration)
				await MainThread.InvokeOnMainThreadAsync(() => SetOuvirRecording(false));
		}
	}

	public void OnOuvirReleased()
	{
		Interlocked.Increment(ref _ouvirPressGeneration);

		if (!_isOuvirRecording && !_recorder.IsRecording)
			return;

		SetOuvirRecording(false);
		_ = Task.Run(StopRecordingAndTranscribeAsync);
	}

	async Task StopRecordingAndTranscribeAsync()
	{
		try
		{
			if (_recorder.IsRecording)
				await _recorder.StopAsync().ConfigureAwait(false);
		}
		catch (Exception)
		{
			/* ignore */
		}

		_ = TranscribeAndApplyVitalsAsync();
	}

	async Task TranscribeAndApplyVitalsAsync()
	{
		if (!File.Exists(_recorder.OutputFilePath))
			return;

		var generation = Interlocked.Increment(ref _transcriptionGeneration);
		SetTranscribing(true);

		try
		{
			var result = await _transcription
				.TranscribeVitalsJsonAsync(_recorder.OutputFilePath)
				.ConfigureAwait(false);

			if (generation != _transcriptionGeneration)
				return;

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (generation != _transcriptionGeneration)
					return;

				if (!result.Success)
				{
					if (!string.IsNullOrWhiteSpace(result.Error))
						await _page.DisplayAlertAsync("Sinais Vitais", result.Error, "OK");
					return;
				}

				await ApplyJsonAsync(result.VitalsJson!);
			});
		}
		finally
		{
			if (generation == _transcriptionGeneration)
				SetTranscribing(false);
		}
	}

	void SetTranscribing(bool transcribing)
	{
		if (_isTranscribing == transcribing)
			return;
		_isTranscribing = transcribing;
		OnPropertyChanged(nameof(IsTranscribing));
		OnPropertyChanged(nameof(OuvirHintText));
	}

	void SetOuvirRecording(bool recording)
	{
		if (_isOuvirRecording == recording)
			return;
		_isOuvirRecording = recording;
		OnPropertyChanged(nameof(IsOuvirRecording));
		OnPropertyChanged(nameof(OuvirHintText));
	}

	public void OnPageAppearing()
	{
		SinaisVitaisDelivery.JsonReceived += OnVitalsJsonReceived;
		_ = TryApplyPendingJsonAsync();
	}

	public void OnPageDisappearing()
	{
		SinaisVitaisDelivery.JsonReceived -= OnVitalsJsonReceived;
		Interlocked.Increment(ref _transcriptionGeneration);
		if (_recorder.IsRecording)
			_ = _recorder.StopAsync();
		SetOuvirRecording(false);
		SetTranscribing(false);
	}

	void OnVitalsJsonReceived(string json) => _ = ApplyJsonAsync(json);

	async Task TryApplyPendingJsonAsync()
	{
		if (!_main.TryConsumePendingSinaisVitaisJson(out var err))
			await _page.DisplayAlertAsync("Sinais Vitais", err ?? "Não foi possível aplicar os sinais vitais.", "OK");
	}

	public async Task<bool> ApplyJsonAsync(string json)
	{
		if (!_main.TryApplySinaisVitaisJson(json, out var err))
		{
			if (err is not null)
				await _page.DisplayAlertAsync("Sinais Vitais", err, "OK");
			return false;
		}

		return true;
	}

	async Task VoltarAsync() => await _page.Navigation.PopModalAsync();

	async Task EnviarAsync()
	{
		var err = Validar();
		if (err is not null)
		{
			await _page.DisplayAlertAsync("Sinais Vitais", err, "OK");
			return;
		}

		_main.MarcarSinaisVitaisBoletimConcluido();
		await _page.DisplayAlertAsync("Sinais Vitais", "Dados guardados em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}

	static string? CampoVazio(string v, string rotulo)
	{
		if (string.IsNullOrWhiteSpace(v))
			return $"Preencha {rotulo}.";
		return null;
	}

	string? Validar()
	{
		return CampoVazio(Form.Pas, "PAS")
			?? CampoVazio(Form.Pad, "PAD")
			?? CampoVazio(Form.Fc, "FC")
			?? CampoVazio(Form.Fr, "FR")
			?? CampoVazio(Form.SatO2, "Sat. O₂")
			?? CampoVazio(Form.TempAx, "Temp.Ax")
			?? CampoVazio(Form.Hgt, "HGT")
			?? CampoVazio(Form.Glasgow, "Glasgow")
			?? CampoVazio(Form.Rts, "RTS")
			?? CampoVazio(Form.Pts, "PTS")
			?? CampoVazio(Form.Evolucao, "Evolução");
	}
}
