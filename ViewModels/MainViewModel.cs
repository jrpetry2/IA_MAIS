using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SaphMovel;
using SaphMovel.Configuration;
using SaphMovel.Models;
using SaphMovel.Services.SinaisVitais;
using SaphMovel.Services.Status;
using SaphMovel.Theming;
using SaphStatus;

namespace SaphMovel.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
	static readonly string[] ValidStatusScenes =
	[
		"Lista",
		"AguardandoRegulacao",
		"Deslocar",
		"AguardandoPartida",
		"Deslocando",
		"AguardandoRemocao",
		"Removendo",
		"NoDestino",
	];

	string _selectedTabKey = "STATUS";
	string _statusScene = "AguardandoRegulacao";

	public string VehicleTitle { get; } = "SA02 CRISTO";
	public string UnitCode { get; } = "IXI 2092";
	public string RegulationStatus { get; } = "Aguardando Regulação";
	public string AvailabilityText { get; } = "Disponível";
	public string PatientEmoji { get; } = "\U0001F915";

	/// <summary>Antes de qualquer “ENVIAR” em Identificação: "Não Identificado". Após sucesso: Nome + ", " + idade (anos) + " " + 1.º char do sexo (F/M).</summary>
	public string PatientSummary
	{
		get => _patientSummary;
		private set
		{
			if (string.Equals(_patientSummary, value, StringComparison.Ordinal))
				return;
			_patientSummary = value;
			OnPropertyChanged(nameof(PatientSummary));
		}
	}

	string _patientSummary = "Não Identificado";
	DateTime _lastVoiceWakeHintUtc;
	string? _resumoIdNome;
	string? _resumoIdIdade;
	string? _resumoIdSexo; // "F" ou "M" — 1.º char mostrado

	/// <summary>Chaves exibidas no Picker de desenvolvimento; depois substitua por estado vindo da API.</summary>
	public ObservableCollection<string> StatusSceneOptions { get; } = new(ValidStatusScenes);

	/// <summary>Quando false, o Picker de troca de cena some (produção).</summary>
	public bool ShowStatusSceneDevPicker { get; set; } = true;

	public bool IsStatusTab => string.Equals(_selectedTabKey, "STATUS", StringComparison.Ordinal);

	/// <summary>Microfone activo para escuta offline (Vosk).</summary>
	public bool IsVoiceMicrophoneActive
	{
		get => _isVoiceMicrophoneActive;
		set
		{
			if (!SetField(ref _isVoiceMicrophoneActive, value))
				return;
			if (value && _voiceModelReadyHint is not null)
			{
				_voiceModelReadyHint = null;
				OnPropertyChanged(nameof(VoiceModelReadyHint));
			}
			OnPropertyChanged(nameof(ShowVoiceModelReadyHint));
			OnPropertyChanged(nameof(ShowVoiceStatusArea));
			OnPropertyChanged(nameof(VoiceStatusBannerBackgroundColor));
		}
	}

	bool _isVoiceMicrophoneActive;

	/// <summary>Mensagem quando a voz offline não está a ouvir (modelo em falta, permissão, etc.).</summary>
	public string? VoiceOfflineHint => _voiceOfflineHint;

	string? _voiceOfflineHint;

	public bool ShowVoiceOfflineHint => !string.IsNullOrWhiteSpace(_voiceOfflineHint);

	/// <summary>Entre modelo OK no disco e microfone a gravar — evita faixa «em branco».</summary>
	public string? VoiceModelReadyHint => _voiceModelReadyHint;

	string? _voiceModelReadyHint;

	public bool ShowVoiceModelReadyHint =>
		!string.IsNullOrWhiteSpace(_voiceModelReadyHint) && !IsVoiceMicrophoneActive && !ShowVoiceOfflineHint;

	/// <summary>Mostra a faixa de estado de voz (modelo pronto, a ouvir ou aviso).</summary>
	public bool ShowVoiceStatusArea =>
		IsVoiceMicrophoneActive || ShowVoiceOfflineHint || ShowVoiceModelReadyHint;

	public Color VoiceStatusBannerBackgroundColor =>
		IsVoiceMicrophoneActive ? Color.FromArgb("#1A2DBE1B")
		: ShowVoiceOfflineHint ? Color.FromArgb("#33FFB020")
		: ShowVoiceModelReadyHint ? Color.FromArgb("#331996F6")
		: Color.FromArgb("#33FFB020");

	public void SetVoiceOfflineHint(string? message)
	{
		var v = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
		if (_voiceOfflineHint == v)
			return;
		_voiceOfflineHint = v;
		if (v is not null)
			_voiceModelReadyHint = null;
		OnPropertyChanged(nameof(VoiceOfflineHint));
		OnPropertyChanged(nameof(VoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceOfflineHint));
		OnPropertyChanged(nameof(ShowVoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceStatusArea));
		OnPropertyChanged(nameof(VoiceStatusBannerBackgroundColor));
	}

	public void SetVoiceModelReadyHint(string? message)
	{
		var v = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
		if (_voiceModelReadyHint == v)
			return;
		_voiceModelReadyHint = v;
		OnPropertyChanged(nameof(VoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceStatusArea));
		OnPropertyChanged(nameof(VoiceStatusBannerBackgroundColor));
	}

	/// <summary>IXI + ponto azul + "Aguardando Regulação" (Chamado, Boletim, Prescrição, Localização).</summary>
	public bool ShowHeaderRegulacaoContext =>
		string.Equals(_selectedTabKey, "CHAMADO", StringComparison.Ordinal)
		|| string.Equals(_selectedTabKey, "BOLETIM", StringComparison.Ordinal)
		|| string.Equals(_selectedTabKey, "PRESCRICAO", StringComparison.Ordinal)
		|| string.Equals(_selectedTabKey, "LOCALIZACAO", StringComparison.Ordinal);

	/// <summary>Compat: só aba BOLETIM (se algum código depender disso).</summary>
	public bool ShowBoletimRegulationLine => string.Equals(_selectedTabKey, "BOLETIM", StringComparison.Ordinal);

	/// <summary>Lista de ações (Disponível + menu).</summary>
	public bool IsListaStatusScene => string.Equals(_statusScene, "Lista", StringComparison.Ordinal);

	/// <summary>Cena "Aguardando regulação" (print: pílula + ilustração, fundo preto).</summary>
	public bool IsAguardandoRegulacaoScene => string.Equals(_statusScene, "AguardandoRegulacao", StringComparison.Ordinal);

	/// <summary>Cenas com herói emoji, prescrição e botão (estados operacionais, exc. lista e regulação).</summary>
	public bool ShowStatusCenasOperacionais => !IsListaStatusScene && !IsAguardandoRegulacaoScene;

	/// <summary>Cabeçalho: linha contextual (ponto + texto) só na aba STATUS fora da lista.</summary>
	public bool ShowStatusContextRow =>
		!ShowHeaderRegulacaoContext
		&& IsStatusTab
		&& !IsListaStatusScene;

	/// <summary>Cabeçalho: IXI + verde + Disponível.</summary>
	public bool ShowStandardDisponivelRow =>
		!ShowHeaderRegulacaoContext && !ShowStatusContextRow;

	/// <summary>
	/// Fundo do corpo: em <b>modo escuro</b>, as abas Chamado/Prescrição/Local usam preto; em modo claro seguem a paleta (não forçar “dark” visual com preferência clara).
	/// </summary>
	public Color MainContentAreaBackgroundColor
	{
		get
		{
			if (SaphThemeController.IsDark && _selectedTabKey is "CHAMADO" or "PRESCRICAO" or "LOCALIZACAO")
				return Color.FromArgb("#000000");
			return TemaAtual()[SaphThemeKeys.BodySurface];
		}
	}

	/// <summary>Cor de corpo de cartão/lista (SaphBodyWhite) — use binding em XAML em vez de DynamicResource para alinhar ao <see cref="SaphThemePalette"/> sem atraso.</summary>
	public Color ThemedBodySurface => TemaAtual()[SaphThemeKeys.BodySurface];

	/// <summary>Aba CHAMADO. Cromo preto (Figma) <b>só</b> com tema escolhido = escuro; com modo claro, o cabeçalho/ abas alinham ao resto da app.</summary>
	public bool IsChamadoTab => string.Equals(_selectedTabKey, "CHAMADO", StringComparison.Ordinal);

	/// <summary>Estilo Figma (preto + texto claro) só se o utilizador tiver o tema escuro activo; evita “o CHAMADO parece pôr tudo a dark” em modo claro.</summary>
	bool IsChamadoTabDarkFigma => IsChamadoTab && SaphThemeController.IsDark;

	public Color MainChromeBackgroundColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#000000")
			: TemaAtual()[SaphThemeKeys.BoletimHeaderMint];

	public Color MainChromePrimaryTextColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#FFFFFF")
			: TemaAtual()[SaphThemeKeys.BoletimTitle];

	public Color MainChromeSecondaryTextColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#8E8E93")
			: TemaAtual()[SaphThemeKeys.BoletimSubtitle];

	public Color MainChromeTabDividerColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#3A3A3C")
			: TemaAtual()[SaphThemeKeys.BoletimTabDivider];

	/// <summary>Pílula de estado (CHAMADO) — fica escura com layout Figma; com modo claro, cinzento alinhado ao claro.</summary>
	public Color ChamadoPillBoxBackground =>
		IsChamadoTab && SaphThemeController.IsDark
			? Color.FromArgb("#2C2C2C")
			: Color.FromArgb("#E5E5E5");

	public string StatusScene
	{
		get => _statusScene;
		set
		{
			var v = string.IsNullOrWhiteSpace(value) ? "Lista" : value;
			if (Array.IndexOf(ValidStatusScenes, v) < 0)
				v = "Lista";
			if (!SetField(ref _statusScene, v))
				return;
			NotifyStatusSceneUi();
		}
	}

	public string SelectedTabKey
	{
		get => _selectedTabKey;
		set
		{
			if (!SetField(ref _selectedTabKey, value))
				return;
			OnPropertyChanged(nameof(ShowBoletimRegulationLine));
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

	public string StatusHeaderContextText => StatusPillText;

	public string StatusPillText => _statusScene switch
	{
		"AguardandoRegulacao" => RegulationStatus,
		"Deslocar" => "Deslocar",
		"AguardandoPartida" => "Aguardando Partida",
		"Deslocando" => "Deslocando",
		"AguardandoRemocao" => "Aguardando partida para Remoção",
		"Removendo" => "Removendo",
		"NoDestino" => "No Destino",
		_ => AvailabilityText,
	};

	/// <summary>Pílula no conteúdo: azul (regulação e cenas) ou vermelho (Deslocar).</summary>
	public Color StatusPillForegroundColor => _statusScene == "Deslocar"
		? Color.FromArgb("#E30613")
		: Color.FromArgb("#2196F3");

	public string StatusHeroGlyph => _statusScene switch
	{
		"Deslocar" => "\U0001F6A8",
		"AguardandoPartida" => "\U0001F468\u200D\u2695\uFE0F",
		"Deslocando" => "\U0001F691",
		"AguardandoRemocao" => "\U0001F691",
		"Removendo" => "\U0001F691",
		"NoDestino" => "\U0001F4CD",
		_ => "\u2713",
	};

	/// <summary>Card “Prescrições” só em Deslocar, Aguardando Partida e Deslocando.</summary>
	public bool ShowStatusPrescriptionBanner =>
		IsStatusTab
		&& !IsListaStatusScene
		&& (_statusScene is "Deslocar" or "AguardandoPartida" or "Deslocando");

	public bool ShowStatusFooterTip =>
		IsStatusTab && string.Equals(_statusScene, "NoDestino", StringComparison.Ordinal);

	public string StatusPrimaryButtonText => _statusScene switch
	{
		"Deslocar" => "👍  CIENTE",
		"AguardandoPartida" => "🚑  CONFIRMAR PARTIDA",
		"Deslocando" => "📍  NO LOCAL",
		"AguardandoRemocao" => "🚑  PARTIDA REMOÇÃO",
		"Removendo" => "📍  NO DESTINO",
		"NoDestino" => "🚩  CONCLUIR A MISSÃO",
		_ => string.Empty,
	};

	public Color StatusPrimaryButtonColor => _statusScene switch
	{
		"Deslocar" => Color.FromArgb("#E30613"),
		"AguardandoPartida" => Color.FromArgb("#2DBE1B"),
		"Deslocando" => Color.FromArgb("#2196F3"),
		"AguardandoRemocao" => Color.FromArgb("#32CD32"),
		"Removendo" => Color.FromArgb("#2196F3"),
		"NoDestino" => Color.FromArgb("#E30613"),
		_ => Color.FromArgb("#757575"),
	};

	public ObservableCollection<BoletimRowItem> BoletimRows { get; } = new();
	public ObservableCollection<StatusMenuItem> MenuItems { get; } = new();
	public ObservableCollection<PrescricaoAtivaItem> PrescricoesAtivas { get; } = new();
	public ObservableCollection<SemPrescricaoItem> SemPrescricaoItens { get; } = new();

	/// <summary>Formulário Identificação guardado em memória (partilhado com a página modal).</summary>
	public IdentificacaoFormData Identificacao { get; } = new();

	/// <summary>Formulário Avaliação Inicial guardado em memória.</summary>
	public AvaliacaoInicialFormData AvaliacaoInicial { get; } = new();

	/// <summary>Formulário Sinais Vitais guardado em memória.</summary>
	public SinaisVitaisFormData SinaisVitais { get; } = new();

	string? _pendingSinaisVitaisJson;

	/// <summary>JSON recebido antes de abrir a modal; aplicado em <see cref="TryConsumePendingSinaisVitaisJson"/>.</summary>
	public void QueueSinaisVitaisJson(string json) => _pendingSinaisVitaisJson = json;

	/// <summary>Aplica JSON ao formulário partilhado (ex.: integração externa).</summary>
	public bool TryApplySinaisVitaisJson(string json, out string? error) =>
		SinaisVitaisJsonMapper.TryApply(SinaisVitais, json, out error);

	/// <summary>Consome JSON em fila, se existir.</summary>
	public bool TryConsumePendingSinaisVitaisJson(out string? error)
	{
		if (string.IsNullOrWhiteSpace(_pendingSinaisVitaisJson))
		{
			error = null;
			return true;
		}

		var json = _pendingSinaisVitaisJson;
		_pendingSinaisVitaisJson = null;
		return TryApplySinaisVitaisJson(json, out error);
	}

	/// <summary>Formulário Avaliação Final guardado em memória.</summary>
	public AvaliacaoFinalFormData AvaliacaoFinal { get; } = new();

	/// <summary>Termo de Responsabilidade em memória (a linha do Boletim não usa ✓ de conclusão).</summary>
	public TermoResponsabilidadeFormData TermoResponsabilidade { get; } = new();

	/// <summary>Impressão do Boletim em memória (a linha do Boletim não usa ✓ de conclusão).</summary>
	public ImpressaoBoletimFormData ImpressaoBoletim { get; } = new();

	bool _boletimIdentificacaoConcluido;
	bool _boletimAvaliacaoInicialConcluido;
	bool _boletimSinaisVitaisConcluido;
	bool _boletimAvaliacaoFinalConcluido;

	/// <summary>Chamado após validação e “ENVIAR” na modal; mostra o ✓ na linha IDENTIFICAÇÃO.</summary>
	public void MarcarIdentificacaoBoletimConcluido()
	{
		_boletimIdentificacaoConcluido = true;
		// Snapshot (evita alterar o resumo com edições ainda não guardadas)
		_resumoIdNome = (Identificacao.Nome ?? "").Trim();
		_resumoIdIdade = (Identificacao.IdadeAnos ?? "").Trim();
		_resumoIdSexo = (Identificacao.Sexo ?? "").Trim();
		ResumoAtualDoPaciente();
		OnPropertyChanged(nameof(ChamadoPatientLine));
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.Identificacao)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	/// <summary>Recalcula o texto exibido acima dos passos do Boletim a partir do snapshot de identificação.</summary>
	public void ResumoAtualDoPaciente()
	{
		if (!_boletimIdentificacaoConcluido)
		{
			PatientSummary = "Não Identificado";
			return;
		}

		var nome = _resumoIdNome ?? "";
		var idade = _resumoIdIdade ?? "";
		var sec = string.IsNullOrEmpty(_resumoIdSexo) ? "" : _resumoIdSexo[0].ToString();
		PatientSummary = $"{nome}, {idade} {sec}".TrimEnd();
	}

	// Aba CHAMADO (dados de demonstração — alimentar pela API)
	public string ChamadoPillText => RegulationStatus;
	public string ChamadoEquipeApoioLabel => "Informações para Equipe de Apoio";
	public string ChamadoEndereco => "Av. Montenegro, 145, PETRÓPOLIS - PORTO ALEGRE";
	public string ChamadoReferencia => "TRUE TECNOLOGIA";
	public string ChamadoAcionamento => "Código 1";
	public string ChamadoNumeroOcorrencia => "333604";
	public string ChamadoAtendimento => "Gravidade: Severa - CLÍNICO/ Síndrome neurológica";
	public string ChamadoOrientacaoRegulacao => "Dor 8/10, sem desconforto respiratório, sem medicação em domicílio.";
	public string ChamadoApoioPor => "--";
	public string ChamadoTipoCliente => "Particular - PJ";
	public string ChamadoCodigoCliente => "137150";
	public string ChamadoDescricaoEvento => "Aguardando Regulação";
	public string ChamadoDataHoraEvento => "06/06/2024 08:41:38";
	public string ChamadoDicaTitulo => "Dica para uma Equipe Campeã!";
	public string ChamadoDicaTexto => "Registre corretamente os comandos dos status do chamado para um tempo resposta melhor.";

	/// <summary>Ex.: “Milton Petry,42 M”. Com identificação concluída, usa o snapshot do Boletim.</summary>
	public string ChamadoPatientLine
	{
		get
		{
			if (!_boletimIdentificacaoConcluido)
				return "Milton Petry,42 M";
			var nome = string.IsNullOrWhiteSpace(_resumoIdNome) ? "Não identificado" : _resumoIdNome.Trim();
			var id = (_resumoIdIdade ?? "").Trim();
			var sex = (_resumoIdSexo ?? "").Trim();
			if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(sex))
				return nome;
			var sec = string.IsNullOrEmpty(sex) ? "" : char.ToUpperInvariant(sex[0]).ToString();
			return string.IsNullOrEmpty(sec) ? $"{nome} ({id})" : $"{nome} ({id} {sec})";
		}
	}

	public void MarcarAvaliacaoInicialBoletimConcluido()
	{
		_boletimAvaliacaoInicialConcluido = true;
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.AvaliacaoInicial)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	public void MarcarSinaisVitaisBoletimConcluido()
	{
		_boletimSinaisVitaisConcluido = true;
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.SinaisVitais)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	public void MarcarAvaliacaoFinalBoletimConcluido()
	{
		_boletimAvaliacaoFinalConcluido = true;
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.AvaliacaoFinal)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	readonly IStatusFacade _statusFacade;

	/// <summary>Device identifier for status API calls (replace with real IMEI from platform services).</summary>
	public string DeviceImei { get; } = "359876050000000";

	public ICommand SelectTabCommand { get; }
	public ICommand HeaderRefreshCommand { get; }
	public ICommand HeaderAlertsCommand { get; }
	public ICommand HeaderMoreCommand { get; }
	public ICommand StatusPrimaryActionCommand { get; }
	public ICommand TapStatusMenuItemCommand { get; }
	public ICommand PrescreverCommand { get; }
	public ICommand ConsumirCommand { get; }
	public ICommand RemoverSemPrescricaoCommand { get; }
	public ICommand VisualizarUltimoRegistroCommand { get; }

	public MainViewModel() : this(NullStatusFacade.Instance) { }

	public MainViewModel(IStatusFacade statusFacade)
	{
		_statusFacade = statusFacade;
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
		PrescreverCommand = new Command(() => _ = PrescreverPlaceholderAsync());
		ConsumirCommand = new Command(() => _ = ConsumirPlaceholderAsync());
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
	}

	private static IReadOnlyDictionary<string, Color> TemaAtual() =>
		SaphThemeController.IsDark ? SaphThemePalette.Dark : SaphThemePalette.Light;

	void OnSaphThemeChanged(object? sender, EventArgs e)
	{
		if (Application.Current?.Dispatcher is { } d)
			d.Dispatch(RefreshThemeBoundChromeColors);
		else
			RefreshThemeBoundChromeColors();
	}

	void RefreshThemeBoundChromeColors()
	{
		OnPropertyChanged(nameof(MainContentAreaBackgroundColor));
		OnPropertyChanged(nameof(MainChromeBackgroundColor));
		OnPropertyChanged(nameof(MainChromePrimaryTextColor));
		OnPropertyChanged(nameof(MainChromeSecondaryTextColor));
		OnPropertyChanged(nameof(MainChromeTabDividerColor));
		OnPropertyChanged(nameof(ThemedBodySurface));
		OnPropertyChanged(nameof(ChamadoPillBoxBackground));
	}

	/// <summary>
	/// Invoca o mesmo fluxo do toque na linha do menu STATUS (comando de voz).
	/// </summary>
	public bool TryInvokeMenuItemFromVoiceCaption(string menuTitleOriginal)
	{
		foreach (var item in MenuItems)
		{
			if (!string.Equals(item.Title.Trim(), menuTitleOriginal.Trim(), StringComparison.OrdinalIgnoreCase))
				continue;
			_ = ShowStatusMenuItemClickPlaceholderAsync(item);
			return true;
		}
		return false;
	}

	/// <summary>Feedback quando a wake foi ouvida mas falta o comando do menu (debounced).</summary>
	public void NotifyVoiceWakeWithoutCommandDebounced()
	{
		var now = DateTime.UtcNow;
		if (now - _lastVoiceWakeHintUtc < TimeSpan.FromSeconds(14))
			return;
		_lastVoiceWakeHintUtc = now;
		_ = ShowVoiceWakeFollowUpHintAsync();
	}

	static async Task ShowVoiceWakeFollowUpHintAsync()
	{
		await ShowAlertFromAnyThreadAsync(
			"Voz",
			$"«{VoskVoiceConfig.WakePhrase}» foi reconhecido. Diga em seguida o comando, por exemplo: Solicitar Abastecimento.")
			.ConfigureAwait(false);
	}

	/// <summary>Apenas feedback de UI; sem chamada à API (integração futura).</summary>
	async Task ShowStatusMenuItemClickPlaceholderAsync(StatusMenuItem item)
	{
		const string body =
			"Clique registado. O envio desta ação ao servidor ainda não está ativo nesta versão.";
		await ShowAlertFromAnyThreadAsync(item.Title, body).ConfigureAwait(false);
	}

	async Task OnStatusPrimaryActionAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is null)
			return;
		var title = string.IsNullOrWhiteSpace(StatusPrimaryButtonText)
			? "Ação STATUS"
			: StatusPrimaryButtonText.Trim();
		await page.DisplayAlertAsync(title, PrimarySceneTapMessage(), "OK");
	}

	/// <summary>Corpo do alerta ao tocar no botão principal da cena STATUS (fora da lista).</summary>
	string PrimarySceneTapMessage() => _statusScene switch
	{
		"Deslocar" => "Confirmação de ciência do deslocamento. Quando integrado, este passo será enviado à API.",
		"AguardandoPartida" => "Partida da unidade confirmada em fluxo local. Integração com API pendente.",
		"Deslocando" => "Indicação de chegada ao local da ocorrência. Integração com API pendente.",
		"AguardandoRemocao" => "Partida para remoção confirmada em fluxo local. Integração com API pendente.",
		"Removendo" => "Chegada ao destino indicada em fluxo local. Integração com API pendente.",
		"NoDestino" => "Conclusão da missão registada localmente. Confirme o Boletim APH antes do envio definitivo.",
		_ => $"Estado atual: {_statusScene}. Ação ainda por integrar à API.",
	};

	void NotifyStatusSceneUi()
	{
		OnPropertyChanged(nameof(IsListaStatusScene));
		OnPropertyChanged(nameof(IsAguardandoRegulacaoScene));
		OnPropertyChanged(nameof(ShowStatusCenasOperacionais));
		OnPropertyChanged(nameof(ShowStatusContextRow));
		OnPropertyChanged(nameof(ShowStandardDisponivelRow));
		OnPropertyChanged(nameof(StatusHeaderContextText));
		OnPropertyChanged(nameof(StatusPillText));
		OnPropertyChanged(nameof(StatusPillForegroundColor));
		OnPropertyChanged(nameof(StatusHeroGlyph));
		OnPropertyChanged(nameof(ShowStatusPrescriptionBanner));
		OnPropertyChanged(nameof(ShowStatusFooterTip));
		OnPropertyChanged(nameof(StatusPrimaryButtonText));
		OnPropertyChanged(nameof(StatusPrimaryButtonColor));
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

	static async Task ShowMoreAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is null)
			return;
		var result = await page.DisplayActionSheetAsync(
			"Opções",
			"Cancelar",
			null,
			"Modo escuro (DARK)",
			"Modo claro",
			"Perfil",
			"Sobre");
		if (result == "Modo escuro (DARK)")
			SaphThemeController.Apply(true);
		else if (result == "Modo claro")
			SaphThemeController.Apply(false);
	}

	void FillPrescricaoDados()
	{
		PrescricoesAtivas.Clear();
		PrescricoesAtivas.Add(new PrescricaoAtivaItem
		{
			Id = "1",
			TituloCategoria = "TRANSPORTE",
			DetalheLinha = "→ BOMBA INFUSÃO - 1 un",
			Pendente = true,
		});
		PrescricoesAtivas.Add(new PrescricaoAtivaItem
		{
			Id = "2",
			TituloCategoria = "RCP + USO DO DEA",
			DetalheLinha = "→ DESFIBRILADOR - 1 un",
			Pendente = true,
		});
		PrescricoesAtivas.Add(new PrescricaoAtivaItem
		{
			Id = "3",
			TituloCategoria = "TRANSPORTE",
			DetalheLinha = "→ BOMBA INFUSÃO - 1 un",
			Pendente = true,
		});
		PrescricoesAtivas.Add(new PrescricaoAtivaItem
		{
			Id = "4",
			TituloCategoria = "RCP + USO DO DEA",
			DetalheLinha = "→ DESFIBRILADOR - 1 un",
			Pendente = true,
		});

		SemPrescricaoItens.Clear();
		SemPrescricaoItens.Add(new SemPrescricaoItem
		{
			Id = "s1",
			Texto = "ADENOSINA 3mg/ml ampola 2ml - 1 amp",
		});
		SemPrescricaoItens.Add(new SemPrescricaoItem
		{
			Id = "s2",
			Texto = "Escopolamina + dipirona 4mg/ml + 500mg/ml - 1 amp",
		});
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

	async Task VisualizarUltimoRegistroAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is not null)
			await page.DisplayAlertAsync("Equipe de Apoio", "Visualização do último registo (integração com API).", "OK");
	}

	void FillMenu()
	{
		MenuItems.Clear();
		MenuItems.Add(new StatusMenuItem { IconFile = "status_sirene.png", Title = "Solicitar Atendimento", IsEmergency = true, StatusType = EStatusType.SolicitarAtendimento });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_pin.png", Title = "Chegada na Base", StatusType = EStatusType.ChegadaNaBase });
		MenuItems.Add(new StatusMenuItem { IconFile = "informar_equipe.png", Title = "Informar Equipe", StatusType = EStatusType.InformarEquipe });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_ambulancia.png", Title = "Troca de Veículo", StatusType = EStatusType.TrocaDeVeiculo });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_combustivel.png", Title = "Solicitar Abastecimento", StatusType = EStatusType.SolicitarAbastecimento });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_higienizacao.png", Title = "Solicitar Higienização", StatusType = EStatusType.SolicitarHigienizacao });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_manutencao.png", Title = "Solicitar Manutenção", StatusType = EStatusType.SolicitarManutencao });
	}

	/// <summary>Reports the selected menu row to the backend via <see cref="IStatusFacade"/>.</summary>
	public Task SubmitStatusMenuAsync(StatusMenuItem item, CancellationToken cancellationToken = default) =>
		_statusFacade.SubmitDeviceStatusAsync(DeviceImei, item.StatusType, cancellationToken);

	void FillBoletimRows()
	{
		BoletimRows.Clear();
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "IDENTIFICAÇÃO",
			StepKey = BoletimStepKeys.Identificacao,
			IsActionRow = false,
			IsCompleted = _boletimIdentificacaoConcluido,
		});
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "AVALIAÇÃO INICIAL",
			StepKey = BoletimStepKeys.AvaliacaoInicial,
			IsActionRow = false,
			IsCompleted = _boletimAvaliacaoInicialConcluido,
		});
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "SINAIS VITAIS",
			StepKey = BoletimStepKeys.SinaisVitais,
			IsActionRow = false,
			IsCompleted = _boletimSinaisVitaisConcluido,
		});
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "AVALIAÇÃO FINAL",
			StepKey = BoletimStepKeys.AvaliacaoFinal,
			IsActionRow = false,
			IsCompleted = _boletimAvaliacaoFinalConcluido,
		});
		BoletimRows.Add(new BoletimRowItem { Title = "TERMO DE RESPONSABILIDADE", StepKey = BoletimStepKeys.Termo, IsActionRow = true });
		BoletimRows.Add(new BoletimRowItem { Title = "IMPRESSÃO DO BOLETIM", StepKey = BoletimStepKeys.Impressao, IsActionRow = true });
	}

	public async Task RefreshAsync()
	{
		await Task.Delay(400);
		FillMenu();
		FillBoletimRows();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged([CallerMemberName] string? name = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;
		field = value;
		OnPropertyChanged(name);
		return true;
	}
}
