using SaphStatus;

namespace SaphMovel.Models;

public sealed class StatusMenuItem
{
	/// <summary>Reserva para texto/emoji se <see cref="IconFile"/> estiver vazio (lista STATUS usa só PNG via <c>MauiImage</c>).</summary>
	public string Glyph { get; init; } = string.Empty;

	/// <summary>Arquivo em <c>Resources/Images</c> incluído como <c>MauiImage</c> (ex.: <c>status_sirene.png</c>).</summary>
	public string? IconFile { get; init; }

	public string Title { get; init; } = string.Empty;
	public bool IsEmergency { get; init; }

	/// <summary>Value sent to the status API when this row is chosen.</summary>
	public EStatusType StatusType { get; init; }
}
