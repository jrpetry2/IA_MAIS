namespace SaphMovel.Configuration;

/// <summary>API de eventos de demo (painel Windows → app).</summary>
public sealed class EventosApiOptions
{
	public const string DefaultBaseUrl = "http://10.0.2.34:5090";

	public string BaseUrl { get; set; } = DefaultBaseUrl;

	public bool Enabled { get; set; } = true;

	public int PollIntervalMs { get; set; } = 800;
}
