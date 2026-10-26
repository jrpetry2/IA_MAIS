using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;
using SaphMovel.Services.OpenAi;

namespace SaphMovel.ViewModels;

/// <summary>Consumo de materiais por voz: grava a fala, a OpenAI extrai materiais e quantidades e a lista marca-os como consumidos.</summary>
public sealed partial class MainViewModel
{
	public const string VoiceCommandConsumirMateriais = "Consumir materiais";
	public const string VoiceCommandConsumirMaterial = "Consumir material";

	const string ConsumoPerguntaFala = "Diga os materiais consumidos e as quantidades.";

	/// <summary>Pico de amplitude (0–32767) acima do qual se considera que há fala — calibrar no aparelho.</summary>
	const int ConsumoSpeechAmplitude = 2000;

	static readonly TimeSpan ConsumoSilencioParaConcluir = TimeSpan.FromSeconds(2);
	static readonly TimeSpan ConsumoEsperaInicioFala = TimeSpan.FromSeconds(8);
	static readonly TimeSpan ConsumoGravacaoMaxima = TimeSpan.FromSeconds(20);
	static readonly TimeSpan ConsumoPollInterval = TimeSpan.FromMilliseconds(200);

	int _consumoEmCurso;
	bool _showConsumoPopup;
	bool _isConsumoGravando;
	string _consumoStatusText = "";
	CancellationTokenSource? _consumoCancelCts;
	CancellationTokenSource? _consumoConcluirCts;
	ICommand? _consumoConcluirCommand;
	ICommand? _consumoCancelarCommand;

	public bool ShowConsumoPopup => _showConsumoPopup;
	public bool IsConsumoGravando => _isConsumoGravando;
	public string ConsumoStatusText => _consumoStatusText;

	public ICommand ConsumoConcluirCommand =>
		_consumoConcluirCommand ??= new Command(() => TryCancel(Volatile.Read(ref _consumoConcluirCts)));

	public ICommand ConsumoCancelarCommand =>
		_consumoCancelarCommand ??= new Command(() => TryCancel(Volatile.Read(ref _consumoCancelCts)));

	/// <summary>Botão CONSUMIR ou comando de voz «consumir materiais». Fora da thread do Vosk (a gravação pára o Vosk).</summary>
	Task ConsumirMateriaisAsync() => Task.Run(ConsumirMateriaisCoreAsync);

	async Task ConsumirMateriaisCoreAsync()
	{
		if (Interlocked.Exchange(ref _consumoEmCurso, 1) == 1)
			return;

		using var cancelCts = new CancellationTokenSource();
		using var concluirCts = new CancellationTokenSource();
		Volatile.Write(ref _consumoCancelCts, cancelCts);
		Volatile.Write(ref _consumoConcluirCts, concluirCts);
		try
		{
			if (_openAi is null || _recorder is null)
			{
				await ShowSuccessToastAsync("Consumo por voz indisponível (OpenAI ou gravação).").ConfigureAwait(false);
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(() => SelectedTabKey = "PRESCRICAO").ConfigureAwait(false);
			await SetConsumoPopupAsync(true, gravando: false, "A preparar…").ConfigureAwait(false);

			try
			{
				// Sem voskHost: a fala não volta a ligar o Vosk, que a gravação vai parar a seguir.
				await _chamadoSpeech.SpeakTextAsync(ConsumoPerguntaFala, null, cancelCts.Token).ConfigureAwait(false);
			}
			catch
			{
				/* logado no serviço */
			}

			if (cancelCts.IsCancellationRequested)
				return;

			var gravou = await GravarMateriaisAsync(cancelCts.Token, concluirCts.Token).ConfigureAwait(false);
			if (cancelCts.IsCancellationRequested)
			{
				_ = ShowSuccessToastAsync("Consumo cancelado.");
				return;
			}

			if (!gravou)
			{
				await AvisarConsumoAsync("Não ouvi nenhum material. Tente de novo.").ConfigureAwait(false);
				return;
			}

			await SetConsumoPopupAsync(true, gravando: false, "A interpretar os materiais…").ConfigureAwait(false);

			var itens = await MainThread.InvokeOnMainThreadAsync(() =>
				PrescricoesAtivas.Select(i => (i.Id, i.TextoLinha)).ToList()).ConfigureAwait(false);

			var result = await _openAi
				.TranscribeMateriaisConsumoAsync(_recorder.OutputFilePath, itens, cancelCts.Token)
				.ConfigureAwait(false);

			if (cancelCts.IsCancellationRequested)
			{
				_ = ShowSuccessToastAsync("Consumo cancelado.");
				return;
			}

			if (!result.Success)
			{
				await AvisarConsumoAsync("Não consegui interpretar os materiais. Tente de novo.", result.Error)
					.ConfigureAwait(false);
				return;
			}

			if (result.Materiais.Count == 0)
			{
				await AvisarConsumoAsync("Não entendi nenhum material. Tente de novo.").ConfigureAwait(false);
				return;
			}

			var aplicados = await MainThread.InvokeOnMainThreadAsync(() => AplicarMateriaisConsumidos(result.Materiais))
				.ConfigureAwait(false);

			await SetConsumoPopupAsync(false, gravando: false, "").ConfigureAwait(false);
			var fala = "Consumido: " + string.Join("; ", aplicados.Select(a =>
				$"{a.Nome.ToLowerInvariant()}, {a.Quantidade} {(a.Quantidade == 1 ? "unidade" : "unidades")}")) + ".";
			_ = ShowSuccessToastAsync(fala);
			await FalarConsumoAsync(fala).ConfigureAwait(false);
		}
		finally
		{
			Interlocked.CompareExchange(ref _consumoCancelCts, null, cancelCts);
			Interlocked.CompareExchange(ref _consumoConcluirCts, null, concluirCts);
			await SetConsumoPopupAsync(false, gravando: false, "").ConfigureAwait(false);
			Volatile.Write(ref _consumoEmCurso, 0);
		}
	}

	/// <summary>
	/// Grava até ao silêncio depois da fala, ao botão Concluir ou ao limite de tempo. Devolve false se não houve fala ou falhou.
	/// </summary>
	async Task<bool> GravarMateriaisAsync(CancellationToken cancel, CancellationToken concluir)
	{
		if (!await _recorder!.StartAsync().ConfigureAwait(false))
			return false;

		var ouviuFala = false;
		try
		{
			await SetConsumoPopupAsync(true, gravando: true, "A ouvir… diga os materiais e as quantidades.")
				.ConfigureAwait(false);

			var inicio = DateTime.UtcNow;
			var ultimaFala = inicio;
			_recorder.ReadMaxAmplitude(); // a primeira leitura devolve o pico desde o arranque
			while (!cancel.IsCancellationRequested)
			{
				if (concluir.IsCancellationRequested)
					break;

				try
				{
					await Task.Delay(ConsumoPollInterval, concluir).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}

				var agora = DateTime.UtcNow;
				if (_recorder.ReadMaxAmplitude() >= ConsumoSpeechAmplitude)
				{
					ouviuFala = true;
					ultimaFala = agora;
				}

				if (agora - inicio >= ConsumoGravacaoMaxima)
					break;
				if (!ouviuFala && agora - inicio >= ConsumoEsperaInicioFala)
					break;
				if (ouviuFala && agora - ultimaFala >= ConsumoSilencioParaConcluir)
					break;
			}
		}
		finally
		{
			await _recorder.StopAsync().ConfigureAwait(false);
		}

		// Concluir à mão conta como fala: o utilizador pode ter falado baixo para o limiar de amplitude.
		return ouviuFala || concluir.IsCancellationRequested;
	}

	/// <summary>Marca os itens da lista (ou cria-os já consumidos, se não estavam prescritos). Thread da UI.</summary>
	List<(string Nome, int Quantidade)> AplicarMateriaisConsumidos(IReadOnlyList<OpenAiMaterialConsumido> materiais)
	{
		var aplicados = new List<(string, int)>();
		foreach (var m in materiais)
		{
			var item = m.Id is null ? null : PrescricoesAtivas.FirstOrDefault(i => i.Id == m.Id);
			if (item is not null && item.Consumido)
			{
				// Mesmo material em várias linhas: consome primeiro uma que ainda está pendente.
				item = PrescricoesAtivas.FirstOrDefault(i => i.Pendente
					&& string.Equals(i.TextoLinha, item.TextoLinha, StringComparison.OrdinalIgnoreCase)) ?? item;
			}

			if (item is null)
			{
				var nome = string.IsNullOrWhiteSpace(m.Material) ? "MATERIAL" : m.Material.Trim().ToUpperInvariant();
				item = new PrescricaoAtivaItem
				{
					Id = Guid.NewGuid().ToString("N")[..8],
					TituloCategoria = "CONSUMO",
					TextoLinha = $"{nome} {m.Quantidade} un",
				};
				PrescricoesAtivas.Add(item);
				aplicados.Add((nome, m.Quantidade));
			}
			else
			{
				aplicados.Add((string.IsNullOrWhiteSpace(m.Material) ? item.TextoLinha : m.Material.Trim(), m.Quantidade));
			}

			item.MarcarConsumido(m.Quantidade);
		}
		return aplicados;
	}

	async Task AvisarConsumoAsync(string fala, string? detalhe = null)
	{
		await SetConsumoPopupAsync(false, gravando: false, "").ConfigureAwait(false);
		_ = ShowSuccessToastAsync(string.IsNullOrWhiteSpace(detalhe) ? fala : $"{fala} ({detalhe})");
		await FalarConsumoAsync(fala).ConfigureAwait(false);
	}

	async Task FalarConsumoAsync(string fala)
	{
		var resumeVosk = IsVoiceMicrophoneActive ? this : null;
		try
		{
			await _chamadoSpeech.SpeakTextAsync(fala, resumeVosk).ConfigureAwait(false);
		}
		catch
		{
			/* logado no serviço */
		}
	}

	Task SetConsumoPopupAsync(bool visible, bool gravando, string status) =>
		MainThread.InvokeOnMainThreadAsync(() =>
		{
			_consumoStatusText = status;
			_isConsumoGravando = gravando;
			_showConsumoPopup = visible;
			OnPropertyChanged(nameof(ConsumoStatusText));
			OnPropertyChanged(nameof(IsConsumoGravando));
			OnPropertyChanged(nameof(ShowConsumoPopup));
		});

	static void TryCancel(CancellationTokenSource? cts)
	{
		try
		{
			cts?.Cancel();
		}
		catch (ObjectDisposedException)
		{
			/* fluxo já terminou */
		}
	}
}
