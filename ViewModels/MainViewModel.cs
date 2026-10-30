using System.Linq;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;
using SaphMovel.Pages;
using SaphMovel.Services.Audio;
using SaphMovel.Services.Missao;
using SaphMovel.Services.OpenAi;
using SaphMovel.Services.Status;
using SaphMovel.Services.Voice;
using SaphMovel.Theming;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel : ObservableModel
{
	string _selectedTabKey = "STATUS";
	string _statusScene = "Lista";

	public string VehicleTitle { get; } = "SA02 CRISTO";
	public string UnitCode { get; } = "IXI 2092";

	string _regulationStatus = "Disponível";
	public string RegulationStatus
	{
		get => _regulationStatus;
		private set => SetField(ref _regulationStatus, value);
	}

	public string AvailabilityText { get; } = "Disponível";
	public string PatientEmoji { get; } = "\U0001F915";

	public bool IsStatusTab => string.Equals(_selectedTabKey, "STATUS", StringComparison.Ordinal);

	/// <summary>IXI + ponto azul + "Aguardando Regulação" (Chamado, Boletim, Prescrição, Localização).</summary>
	public bool ShowHeaderRegulacaoContext =>
		string.Equals(_selectedTabKey, "CHAMADO", StringComparison.Ordinal)
		|| string.Equals(_selectedTabKey, "BOLETIM", StringComparison.Ordinal)
		|| string.Equals(_selectedTabKey, "PRESCRICAO", StringComparison.Ordinal)
		|| string.Equals(_selectedTabKey, "LOCALIZACAO", StringComparison.Ordinal);

	public string SelectedTabKey
	{
		get => _selectedTabKey;
		set
		{
			if (!SetField(ref _selectedTabKey, value))
				return;
			OnPropertyChanged(nameof(ShowHeaderRegulacaoContext));
			OnPropertyChanged(nameof(IsStatusTab));
			OnPropertyChanged(nameof(ShowStatusContextRow));
			OnPropertyChanged(nameof(ShowStandardDisponivelRow));
			OnPropertyChanged(nameof(MainContentAreaBackgroundColor));
			OnPropertyChanged(nameof(IsChamadoTab));
			OnPropertyChanged(nameof(MainChromeBackgroundColor));
			OnPropertyChanged(nameof(MainChromePrimaryTextColor));
			OnPropertyChanged(nameof(MainChromeSecondaryTextColor));
			OnPropertyChanged(nameof(MainChromeTabDividerColor));
			// Não chamar NotifyStatusSceneUi() aqui: a cena STATUS (_statusScene) não mudou.
			// Isso evitava dezenas de re-bindings/relayouts desnecessários a cada toque de aba (UI trancava).
			OnPropertyChanged(nameof(ShowStatusPrescriptionBanner));
			OnPropertyChanged(nameof(ShowStatusFooterTip));
			OnPropertyChanged(nameof(ThemedBodySurface));
			OnPropertyChanged(nameof(ChamadoPillBoxBackground));
		}
	}

	readonly IStatusFacade _statusFacade;
	readonly IMissaoFacade _missaoFacade;
	readonly IChamadoSpeechService _chamadoSpeech;
	readonly IOpenAiTranscriptionService? _openAi;
	readonly IPressHoldAudioRecordService? _recorder;

	/// <summary>Device identifier for status API calls (replace with real IMEI from platform services).</summary>
	public string DeviceImei { get; } = "359876050000000";

	public ICommand SelectTabCommand { get; }
	public ICommand HeaderRefreshCommand { get; }
	public ICommand HeaderAlertsCommand { get; }
	public ICommand HeaderMoreCommand { get; }
	public ICommand StatusPrimaryActionCommand { get; }
	public ICommand TapStatusMenuItemCommand { get; }
	public ICommand AtualizarEnderecoCommand { get; }
	public ICommand PrescreverCommand { get; }
	public ICommand ConsumirCommand { get; }
	public ICommand RemoverSemPrescricaoCommand { get; }
	public ICommand VisualizarUltimoRegistroCommand { get; }

	public MainViewModel()
		: this(NullStatusFacade.Instance, new MissaoFacade(), NullChamadoSpeechService.Instance, openAi: null) { }

	public MainViewModel(
		IStatusFacade statusFacade,
		IMissaoFacade missaoFacade,
		IChamadoSpeechService chamadoSpeech,
		IOpenAiTranscriptionService? openAi = null,
		IPressHoldAudioRecordService? recorder = null)
	{
		_statusFacade = statusFacade;
		_missaoFacade = missaoFacade;
		_chamadoSpeech = chamadoSpeech;
		_openAi = openAi;
		_recorder = recorder;
		SelectTabCommand = new Command<string>(k => SelectedTabKey = k ?? "STATUS");
		HeaderRefreshCommand = new Command(async () => await RefreshAsync());
		HeaderAlertsCommand = new Command(async () => await ShowAlertsAsync());
		HeaderMoreCommand = new Command(async () => await ShowMoreAsync());
		StatusPrimaryActionCommand = new Command(async () => await OnStatusPrimaryActionAsync());
		TapStatusMenuItemCommand = new Command<object>(o =>
		{
			if (o is not StatusMenuItem item)
				return;
			_ = ShowStatusMenuItemClickPlaceholderAsync(item);
		});
		AtualizarEnderecoCommand = new Command(async () => await OpenAlterarEnderecoAsync());
		PrescreverCommand = new Command(() => _ = PrescreverPlaceholderAsync());
		ConsumirCommand = new Command(() => _ = ConsumirMateriaisAsync());
		RemoverSemPrescricaoCommand = new Command<SemPrescricaoItem?>(i =>
		{
			if (i is null)
				return;
			var o = SemPrescricaoItens.FirstOrDefault(x => x.Id == i.Id);
			if (o is not null)
				SemPrescricaoItens.Remove(o);
		});
		VisualizarUltimoRegistroCommand = new Command(() => _ = VisualizarUltimoRegistroAsync());
		FillBoletimRows();
		FillMenu();
		FillPrescricaoDados();
		ResumoAtualDoPaciente();
		SaphThemeController.ThemeChanged += OnSaphThemeChanged;
		_missaoFacade.Changed += OnMissaoChanged;
		_chamadoSpeech.SpeakingChanged += OnAssistantSpeakingChanged;
		ApplyMissaoSnapshot(_missaoFacade.Atual);
	}

	/// <summary>Larga os eventos globais (tema e missão) — evitar instâncias órfãs a receber notificações.</summary>
	public void Detach()
	{
		SaphThemeController.ThemeChanged -= OnSaphThemeChanged;
		_missaoFacade.Changed -= OnMissaoChanged;
	}

	static async Task ShowAlertFromAnyThreadAsync(string title, string message)
	{
		var app = Application.Current;
		if (app?.Dispatcher is null)
			return;
		await app.Dispatcher.DispatchAsync(async () =>
		{
			var page = UiDialogHost.GetPage();
			if (page is not null)
				await page.DisplayAlertAsync(title, message, "OK");
		}).ConfigureAwait(false);
	}

	static async Task ShowAlertsAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is not null)
			await page.DisplayAlertAsync("Alertas", "Nenhum alerta pendente.", "OK");
	}

	async Task ShowMoreAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is null)
			return;
		var result = await page.DisplayActionSheetAsync(
			"Opções",
			"Cancelar",
			null,
			"Simular eventos",
			"Voz do assistente",
			"Modo escuro (DARK)",
			"Modo claro",
			"Perfil",
			"Sobre");
		if (result == "Simular eventos")
		{
			var sim = new DevEventSimulatorPage(_missaoFacade, this);
			await page.Navigation.PushModalAsync(sim);
		}
		else if (result == "Voz do assistente")
		{
			var picker = new TtsVoicePickerPage(_chamadoSpeech);
			await page.Navigation.PushModalAsync(picker);
		}
		else if (result == "Modo escuro (DARK)")
			SaphThemeController.Apply(true);
		else if (result == "Modo claro")
			SaphThemeController.Apply(false);
	}

	public async Task RefreshAsync()
	{
		await Task.Delay(400);
		FillMenu();
		FillBoletimRows();
	}
}
