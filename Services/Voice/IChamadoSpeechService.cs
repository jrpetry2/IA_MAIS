using SaphEventos;
using SaphMovel.Models;
using SaphMovel.ViewModels;

namespace SaphMovel.Services.Voice;

/// <summary>Lê chamado e prescritões em voz alta (TTS nativo MAUI/Android).</summary>
public interface IChamadoSpeechService
{
	/// <summary>True enquanto o TTS está a falar (permite barge-in «pare»).</summary>
	bool IsSpeaking { get; }

	/// <summary>Instante (UTC) em que a última fala terminou — usado para descartar o eco do TTS no Vosk.</summary>
	DateTime LastSpeechEndedUtc { get; }

	/// <param name="voskHost">Se a escuta Vosk estiver activa, mantém o microfone a ouvir para «pare» durante a fala.</param>
	Task SpeakChamadoAsync(ChamadoPayload payload, MainViewModel? voskHost = null, CancellationToken cancellationToken = default);

	Task SpeakPrescricaoAsync(PrescricaoPacote pacote, MainViewModel? voskHost = null, string? nomeEquipe = null, CancellationToken cancellationToken = default);

	/// <summary>Lê texto arbitrário (ex.: pergunta sim/não ou procedimento de protocolo).</summary>
	Task SpeakTextAsync(string text, MainViewModel? voskHost = null, CancellationToken cancellationToken = default);

	void Cancel();

	Task<IReadOnlyList<TtsVoiceOption>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default);

	TtsVoiceOption? GetSelectedVoiceOrNull(IReadOnlyList<TtsVoiceOption> available);

	void SelectVoice(TtsVoiceOption? voice);

	Task SpeakPreviewAsync(string text, TtsVoiceOption? voice = null, CancellationToken cancellationToken = default);
}

public sealed class TtsVoiceOption
{
	public required string Id { get; init; }
	public required string Language { get; init; }
	public string Country { get; init; } = "";
	public required string Name { get; init; }
	public string DisplayTitle => string.IsNullOrWhiteSpace(Name)
		? $"{Language}{(string.IsNullOrWhiteSpace(Country) ? "" : $"-{Country}")}"
		: Name;
	public string DisplaySubtitle =>
		$"{Language}{(string.IsNullOrWhiteSpace(Country) ? "" : $"-{Country}")}"
		+ (string.IsNullOrWhiteSpace(Id) ? "" : $" · {Id}");
}
