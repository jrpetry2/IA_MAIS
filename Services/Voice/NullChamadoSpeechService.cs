using SaphEventos;
using SaphMovel.Models;
using SaphMovel.ViewModels;

namespace SaphMovel.Services.Voice;

public sealed class NullChamadoSpeechService : IChamadoSpeechService
{
	public static NullChamadoSpeechService Instance { get; } = new();

	public bool IsSpeaking => false;

	public DateTime LastSpeechEndedUtc => DateTime.MinValue;

	public Task SpeakChamadoAsync(
		ChamadoPayload payload,
		MainViewModel? voskHost = null,
		CancellationToken cancellationToken = default) =>
		Task.CompletedTask;

	public Task SpeakPrescricaoAsync(
		PrescricaoPacote pacote,
		MainViewModel? voskHost = null,
		string? nomeEquipe = null,
		CancellationToken cancellationToken = default) =>
		Task.CompletedTask;

	public Task SpeakTextAsync(
		string text,
		MainViewModel? voskHost = null,
		CancellationToken cancellationToken = default) =>
		Task.CompletedTask;

	public void Cancel() { }

	public Task<IReadOnlyList<TtsVoiceOption>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<TtsVoiceOption>>(Array.Empty<TtsVoiceOption>());

	public TtsVoiceOption? GetSelectedVoiceOrNull(IReadOnlyList<TtsVoiceOption> available) => null;

	public void SelectVoice(TtsVoiceOption? voice) { }

	public Task SpeakPreviewAsync(
		string text,
		TtsVoiceOption? voice = null,
		CancellationToken cancellationToken = default) =>
		Task.CompletedTask;
}
