using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using SaphMovel;
using SaphMovel.Models;
using SaphMovel.Services.Voice;
using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class MainPage : ContentPage
{
	readonly ILogger<MainPage> _log;
	readonly IVoskListeningService _vosk;
	bool _mapaLocalizacaoPronto;

	public MainPage(ILogger<MainPage> log, IVoskListeningService vosk)
	{
		_log = log;
		_vosk = vosk;
		InitializeComponent();
		UiDialogHost.Register(this);
		BindingContext = AppServices.CreateMainViewModel();
		MapaLocalizacao.HandlerChanged += OnMapaLocalizacaoHandlerChanged;
		_vosk.ListeningActiveChanged += OnVoskListeningActiveChanged;
	}

	void OnVoskListeningActiveChanged(object? sender, bool active)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (BindingContext is MainViewModel vm)
				vm.IsVoiceMicrophoneActive = active;
		});
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		try
		{
			if (BindingContext is MainViewModel vm)
				await _vosk.StartAsync(vm).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_log.LogDebug(ex, "Vosk: arranque.");
		}
	}

	protected override async void OnDisappearing()
	{
		try
		{
			await _vosk.StopAsync().ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_log.LogDebug(ex, "Vosk: paragem.");
		}
		base.OnDisappearing();
	}

	void OnMapaLocalizacaoHandlerChanged(object? sender, EventArgs e)
	{
		if (_mapaLocalizacaoPronto || MapaLocalizacao.Handler is null)
			return;
		_mapaLocalizacaoPronto = true;
		try
		{
			// Av. Montenegro, 145 — Petrópolis, Porto Alegre (área aproximada)
			const double lat = -30.0210;
			const double lon = -51.1933;
			var center = new Location(lat, lon);
			MapaLocalizacao.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(0.55)));
			if (MapaLocalizacao.Pins.Count == 0)
			{
				MapaLocalizacao.Pins.Add(
					new Pin
					{
						Type = PinType.Place,
						Label = "Av. Montenegro, 145 -",
						Address = "PETRÓPOLIS -" + System.Environment.NewLine + "PORTO ALEGRE",
						Location = center,
					});
			}
		}
		catch (Exception ex)
		{
			_log.LogDebug(ex, "Inicializar mapa (Google Maps / GMS).");
		}
	}

	async void OnRefreshing(object? sender, EventArgs e)
	{
		try
		{
			if (BindingContext is MainViewModel vm)
				await vm.RefreshAsync();
		}
		finally
		{
			if (sender is RefreshView rv)
				rv.IsRefreshing = false;
		}
	}

	async void OnBoletimRowSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (sender is not CollectionView cv)
			return;
		if (e.CurrentSelection.FirstOrDefault() is not BoletimRowItem row)
			return;
		cv.SelectedItem = null;
		if (BindingContext is not MainViewModel vm)
		{
			await DisplayAlertAsync(row.Title, "Integrar fluxo desta etapa.", "OK");
			return;
		}

		if (row.StepKey == BoletimStepKeys.Identificacao)
		{
			await Navigation.PushModalAsync(new IdentificacaoPage(vm));
			return;
		}

		if (row.StepKey == BoletimStepKeys.AvaliacaoInicial)
		{
			await Navigation.PushModalAsync(new AvaliacaoInicialPage(vm));
			return;
		}

		if (row.StepKey == BoletimStepKeys.SinaisVitais)
		{
			await Navigation.PushModalAsync(new SinaisVitaisPage(vm));
			return;
		}

		if (row.StepKey == BoletimStepKeys.AvaliacaoFinal)
		{
			await Navigation.PushModalAsync(new AvaliacaoFinalPage(vm));
			return;
		}

		if (row.StepKey == BoletimStepKeys.Termo)
		{
			await Navigation.PushModalAsync(new TermoResponsabilidadePage(vm));
			return;
		}

		if (row.StepKey == BoletimStepKeys.Impressao)
		{
			await Navigation.PushModalAsync(new ImpressaoBoletimPage(vm));
			return;
		}

		await DisplayAlertAsync(row.Title, "Integrar fluxo desta etapa.", "OK");
	}
}
