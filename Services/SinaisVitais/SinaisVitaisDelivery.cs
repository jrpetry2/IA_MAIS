using SaphMovel.Pages;
using SaphMovel.ViewModels;

namespace SaphMovel.Services.SinaisVitais;

/// <summary>Entrega de JSON de sinais vitais para a tela (fila, evento ou Intent).</summary>
public static class SinaisVitaisDelivery
{
	/// <summary>Disparado na thread de UI quando chega JSON (modal aberta pode aplicar de imediato).</summary>
	public static event Action<string>? JsonReceived;

	public static void PublishJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return;

		if (TryGetMainViewModel() is { } vm)
			vm.QueueSinaisVitaisJson(json);

		JsonReceived?.Invoke(json);
	}

	static MainViewModel? TryGetMainViewModel()
	{
		try
		{
			var mainPage = AppServices.Services.GetService<MainPage>();
			if (mainPage?.BindingContext is MainViewModel vm)
				return vm;
		}
		catch (InvalidOperationException)
		{
			/* AppServices ainda não inicializado */
		}

		return null;
	}
}
