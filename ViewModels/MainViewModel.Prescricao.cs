using System.Collections.ObjectModel;
using System.Threading.Channels;
using Microsoft.Maui.Controls;
using SaphMovel.Models;
using SaphMovel.Services.OpenAi;
using SaphMovel.Services.Voice;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel
{
	public ObservableCollection<PrescricaoAtivaItem> PrescricoesAtivas { get; } = new();
	public ObservableCollection<SemPrescricaoItem> SemPrescricaoItens { get; } = new();

	/// <summary>Frases Vosk recebidas enquanto se espera sim/não (várias por janela — ruído ou eco não esgotam a pergunta).</summary>
	Channel<string>? _protocolYesNoUtterances;
	int _protocolConfirmGeneration;

	static readonly TimeSpan YesNoAiTimeout = TimeSpan.FromSeconds(4);

	/// <summary>Activa o pacote PCR (compatibilidade com simulador / chamadas antigas).</summary>
	public void AtivarProtocoloPcr() => AtivarProtocolo(ProtocoloAtendimentoId.Pcr);

	public void AtivarProtocoloAfogamento() => AtivarProtocolo(ProtocoloAtendimentoId.Afogamento);

	public void AtivarProtocoloAtropelamento() => AtivarProtocolo(ProtocoloAtendimentoId.Atropelamento);

	public void AtivarProtocolo(ProtocoloAtendimentoId id)
	{
		var def = ProtocoloAtendimentoCatalog.Get(id);
		async Task RunAsync()
		{
			var pacote = def.CreatePacote();
			ApplyPrescricaoPacote(pacote, switchToTab: true, announce: false);
			await Task.Delay(200).ConfigureAwait(false);
			await ShowSuccessToastAsync(def.ToastAtivado).ConfigureAwait(false);

			var voskHost = IsVoiceMicrophoneActive ? this : null;
			try
			{
				await _chamadoSpeech.SpeakPrescricaoAsync(pacote, voskHost, VehicleTitle).ConfigureAwait(false);
			}
			catch
			{
				/* logado no serviço */
			}

			await AskRelembrarProcedimentoAsync(def).ConfigureAwait(false);
		}

		if (MainThread.IsMainThread)
			_ = RunAsync();
		else
			MainThread.BeginInvokeOnMainThread(() => _ = RunAsync());
	}

	/// <summary>
	/// Consome a próxima frase Vosk enquanto se espera sim/não do protocolo (sem wake).
	/// </summary>
	public bool TryHandleProtocolYesNoUtterance(string utterance)
	{
		var channel = Volatile.Read(ref _protocolYesNoUtterances);
		if (channel is null || string.IsNullOrWhiteSpace(utterance))
			return false;
		return channel.Writer.TryWrite(utterance.Trim());
	}

	async Task AskRelembrarProcedimentoAsync(ProtocoloAtendimentoDefinicao def)
	{
		var gen = Interlocked.Increment(ref _protocolConfirmGeneration);
		const string pergunta = ProtocoloAtendimentoCatalog.PerguntaRelembrarProcedimento;
		var voskHost = IsVoiceMicrophoneActive ? this : null;

		try
		{
			await _chamadoSpeech.SpeakTextAsync(pergunta, voskHost).ConfigureAwait(false);
		}
		catch
		{
			/* logado no serviço */
		}

		if (gen != _protocolConfirmGeneration)
			return;

		bool? resposta = null;
		if (IsVoiceMicrophoneActive)
		{
			_ = ShowSuccessToastAsync("Diga sim ou não…", visibleMs: 3500);
			resposta = await TryInterpretVoiceYesNoAsync(pergunta, TimeSpan.FromSeconds(10), gen)
				.ConfigureAwait(false);
		}

		if (gen != _protocolConfirmGeneration)
			return;

		if (resposta is null)
			resposta = await ShowRelembrarAlertAsync().ConfigureAwait(false);

		if (gen != _protocolConfirmGeneration)
			return;

		if (resposta != true)
			return;

		var resumeVosk = IsVoiceMicrophoneActive ? this : null;
		try
		{
			await _chamadoSpeech
				.SpeakTextAsync(def.ProcedimentoFala, resumeVosk)
				.ConfigureAwait(false);
		}
		catch
		{
			/* logado no serviço */
		}
	}

	/// <summary>
	/// Ouve até <paramref name="timeout"/>: cada frase é avaliada localmente (offline) e, se ambígua, pela IA.
	/// Eco da pergunta e respostas indefinidas não encerram a espera. Devolve null se ninguém respondeu.
	/// </summary>
	async Task<bool?> TryInterpretVoiceYesNoAsync(string pergunta, TimeSpan timeout, int gen)
	{
		var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
		Volatile.Write(ref _protocolYesNoUtterances, channel);
		using var cts = new CancellationTokenSource(timeout);
		try
		{
			while (true)
			{
				string fala;
				try
				{
					fala = await channel.Reader.ReadAsync(cts.Token).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					return null;
				}

				if (gen != _protocolConfirmGeneration)
					return null;

				if (VoiceYesNoMatcher.IsLikelyPromptEcho(fala))
					continue;

				switch (VoiceYesNoMatcher.Match(fala))
				{
					case VoiceYesNoAnswer.Sim:
						return true;
					case VoiceYesNoAnswer.Nao:
						return false;
				}

				if (_openAi is null)
					continue;

				using var aiCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
				aiCts.CancelAfter(YesNoAiTimeout);
				var result = await _openAi.InterpretYesNoAsync(pergunta, fala, aiCts.Token)
					.ConfigureAwait(false);
				if (gen != _protocolConfirmGeneration)
					return null;
				if (result.Success && result.Answer == OpenAiYesNoAnswer.Sim)
					return true;
				if (result.Success && result.Answer == OpenAiYesNoAnswer.Nao)
					return false;
				if (cts.IsCancellationRequested)
					return null;
			}
		}
		finally
		{
			Interlocked.CompareExchange(ref _protocolYesNoUtterances, null, channel);
			channel.Writer.TryComplete();
		}
	}

	static async Task<bool?> ShowRelembrarAlertAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is null)
			return null;

		return await MainThread.InvokeOnMainThreadAsync(async () =>
			await page.DisplayAlertAsync(
				"Protocolo",
				ProtocoloAtendimentoCatalog.PerguntaRelembrarProcedimento,
				"Sim",
				"Não")).ConfigureAwait(false);
	}

	void FillPrescricaoDados() =>
		ApplyPrescricaoPacote(DemoPrescricaoFactory.Inicial(), switchToTab: false, announce: false);

	/// <summary>Simula recebimento de prescritões (substitui as listas actuais).</summary>
	public void ApplyPrescricaoPacote(PrescricaoPacote pacote, bool switchToTab = true, bool announce = true)
	{
		PrescricoesAtivas.Clear();
		foreach (var item in pacote.Ativas)
			PrescricoesAtivas.Add(item);

		SemPrescricaoItens.Clear();
		foreach (var item in pacote.SemPrescricao)
			SemPrescricaoItens.Add(item);

		if (switchToTab)
			SelectedTabKey = "PRESCRICAO";

		if (announce && switchToTab)
		{
			var resumeVosk = IsVoiceMicrophoneActive ? this : null;
			_ = SpeakPrescricaoAsync(pacote, resumeVosk);
		}
	}

	async Task SpeakPrescricaoAsync(PrescricaoPacote pacote, MainViewModel? voskHost)
	{
		try
		{
			await _chamadoSpeech
				.SpeakPrescricaoAsync(pacote, voskHost, VehicleTitle)
				.ConfigureAwait(false);
		}
		catch
		{
			/* logado no serviço */
		}
	}

	public void ClearPrescricoes(bool switchToTab = true)
	{
		PrescricoesAtivas.Clear();
		SemPrescricaoItens.Clear();
		if (switchToTab)
			SelectedTabKey = "PRESCRICAO";
	}

	async Task PrescreverPlaceholderAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is not null)
			await page.DisplayAlertAsync("Prescrição", "Fluxo PRESCREVER (integração com API).", "OK");
	}

	async Task ConsumirPlaceholderAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is not null)
			await page.DisplayAlertAsync("Prescrição", "Fluxo CONSUMIR (integração com API).", "OK");
	}
}
