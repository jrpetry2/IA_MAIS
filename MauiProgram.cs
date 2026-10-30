using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Controls.Hosting;
using SaphMovel.Configuration;
using SaphMovel.Services.Audio;
using SaphMovel.Services.Demo;
using SaphMovel.Services.Missao;
using SaphMovel.Services.OpenAi;
using SaphMovel.Services.Status;
using SaphMovel.Services.Voice;
#if ANDROID
using SaphMovel.Pages;
using SaphMovel.Platforms.Android.Audio;
using SaphMovel.Platforms.Android.Voice;
#endif
using SaphMovel.ViewModels;

namespace SaphMovel;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiMaps()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddLogging();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.Configure<StatusApiOptions>(o =>
		{
#if ANDROID
			o.BaseUrl = RedeConfig.StatusBaseUrl;
#else
			o.BaseUrl = "http://localhost:5088";
#endif
		});

		builder.Services.Configure<EventosApiOptions>(o =>
		{
			o.BaseUrl = EventosApiOptions.DefaultBaseUrl;
			o.Enabled = true;
			o.PollIntervalMs = 800;
		});

		builder.Services.AddHttpClient<DemoEventRemoteSyncService>((sp, client) =>
		{
			var baseUrl = sp.GetRequiredService<IOptions<EventosApiOptions>>().Value.BaseUrl.TrimEnd('/');
			client.BaseAddress = new Uri(baseUrl + "/");
			client.Timeout = TimeSpan.FromSeconds(12);
		});

		builder.Services.Configure<OpenAiTranscriptionOptions>(o =>
		{
			o.ApiKey = "";
		});

		builder.Services.AddHttpClient<IOpenAiTranscriptionService, OpenAiTranscriptionService>(client =>
		{
			client.Timeout = TimeSpan.FromMinutes(3);
			client.DefaultRequestHeaders.ExpectContinue = false;
		});

		builder.Services.AddHttpClient<IStatusFacade, StatusFacade>((sp, client) =>
		{
			var baseUrl = sp.GetRequiredService<IOptions<StatusApiOptions>>().Value.BaseUrl.TrimEnd('/');
			client.BaseAddress = new Uri(baseUrl + "/");
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		builder.Services.AddSingleton<IMissaoFacade, MissaoFacade>();

#if ANDROID
		builder.Services.AddSingleton<IVoskListeningService, VoskListeningService>();
		builder.Services.AddSingleton<IPressHoldAudioRecordService, SinaisVitaisPressRecordService>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<AppShell>();
#endif

		builder.Services.AddSingleton<IChamadoSpeechService>(sp =>
		{
			var log = sp.GetRequiredService<ILogger<ChamadoSpeechService>>();
			var vosk = sp.GetService<IVoskListeningService>();
			return new ChamadoSpeechService(log, vosk);
		});

		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<DevEventSimulatorPage>();
		builder.Services.AddTransient<TtsVoicePickerPage>();

		var app = builder.Build();
		AppServices.Initialize(app.Services);
		return app;
	}
}
