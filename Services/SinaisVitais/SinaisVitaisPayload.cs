using System.Text.Json.Serialization;

namespace SaphMovel.Services.SinaisVitais;

/// <summary>Payload de sinais vitais (ex.: dispositivo / integração externa).</summary>
public sealed class SinaisVitaisPayload
{
	[JsonPropertyName("PAS")]
	public double? Pas { get; set; }

	[JsonPropertyName("PAD")]
	public double? Pad { get; set; }

	[JsonPropertyName("FC")]
	public double? Fc { get; set; }

	[JsonPropertyName("FR")]
	public double? Fr { get; set; }

	[JsonPropertyName("Sat")]
	public double? Sat { get; set; }

	[JsonPropertyName("Temp")]
	public double? Temp { get; set; }

	[JsonPropertyName("HGT")]
	public double? Hgt { get; set; }

	[JsonPropertyName("Glasgow")]
	public double? Glasgow { get; set; }

	[JsonPropertyName("RTS")]
	public double? Rts { get; set; }

    [JsonPropertyName("PTS")]
    public double? Pts { get; set; }
    [JsonPropertyName("Evolucao")]
    public string? Evolucao { get; set; }
}
