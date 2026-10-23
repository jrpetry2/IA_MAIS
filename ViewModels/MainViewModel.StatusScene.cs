using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel
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

	/// <summary>Chaves exibidas no Picker de desenvolvimento; depois substitua por estado vindo da API.</summary>
	public ObservableCollection<string> StatusSceneOptions { get; } = new(ValidStatusScenes);

	/// <summary>Quando false, o Picker de troca de cena some (produção).</summary>
	public bool ShowStatusSceneDevPicker { get; set; } = true;

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
		? ResourceColor("SaphStatusActionRed", "#E30613")
		: ResourceColor("SaphStatusBlue", "#2196F3");

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
		"Deslocar" => ResourceColor("SaphStatusActionRed", "#E30613"),
		"AguardandoPartida" => ResourceColor("SaphStatusGreenGo", "#2DBE1B"),
		"Deslocando" => ResourceColor("SaphStatusBlue", "#2196F3"),
		"AguardandoRemocao" => ResourceColor("SaphStatusGreenAlt", "#32CD32"),
		"Removendo" => ResourceColor("SaphStatusBlue", "#2196F3"),
		"NoDestino" => ResourceColor("SaphStatusActionRed", "#E30613"),
		_ => ResourceColor("SaphStatusUnknown", "#757575"),
	};

	async Task OnStatusPrimaryActionAsync()
	{
		if (IsAguardandoRegulacaoScene)
		{
			var page = UiDialogHost.GetPage();
			if (page is not null)
				await page.DisplayAlertAsync("Aguardando Regulação", "Aguarde a decisão do Cliente (use o simulador: Remover).", "OK");
			return;
		}

		if (_missaoFacade.TryAvancarProximo(out _))
			return;

		var host = UiDialogHost.GetPage();
		if (host is null)
			return;
		var title = string.IsNullOrWhiteSpace(StatusPrimaryButtonText)
			? "Ação STATUS"
			: StatusPrimaryButtonText.Trim();
		await host.DisplayAlertAsync(title, PrimarySceneTapMessage(), "OK");
	}

	/// <summary>Corpo do alerta ao tocar no botão principal da cena STATUS (fora da lista).</summary>
	string PrimarySceneTapMessage() => _statusScene switch
	{
		"Deslocar" => "Confirmação de ciência do deslocamento.",
		"AguardandoPartida" => "Partida da unidade confirmada.",
		"Deslocando" => "Indicação de chegada ao local da ocorrência.",
		"AguardandoRemocao" => "Partida para remoção confirmada.",
		"Removendo" => "Chegada ao destino indicada.",
		"NoDestino" => "Conclusão da missão. Confirme o Boletim APH antes do envio definitivo.",
		_ => $"Estado atual: {_statusScene}.",
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
		OnPropertyChanged(nameof(ShowAtualizarEnderecoButton));
	}
}
