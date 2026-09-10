using SaphMovel.ViewModels;

namespace SaphMovel.Services.Voice;

/// <summary>Escuta contínua offline (Vosk no Android). Extrai «wake» + comando e delega ao <see cref="MainViewModel"/>.</summary>
public interface IVoskListeningService
{
	/// <summary>Dispara quando o microfone entra ou sai de captura activa (escuta offline).</summary>
	event EventHandler<bool>? ListeningActiveChanged;

	Task StartAsync(MainViewModel viewModel, CancellationToken cancellationToken = default);

	Task StopAsync();
}
