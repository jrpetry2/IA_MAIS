namespace SaphMovel.Configuration;

/// <summary>API de eventos de demo (painel Windows → app).</summary>
public sealed class EventosApiOptions
{
	public static string DefaultBaseUrl => RedeConfig.EventosBaseUrl;

	public string BaseUrl { get; set; } = DefaultBaseUrl;

	public bool Enabled { get; set; } = true;

	public int PollIntervalMs { get; set; } = 800;
}
