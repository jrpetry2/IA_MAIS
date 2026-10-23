using SaphMovel.ViewModels;

namespace SaphMovel.Services.Voice;

/// <summary>Escuta contínua offline (Vosk no Android). Extrai «wake» + comando e delega ao <see cref="MainViewModel"/>.</summary>
public interface IVoskListeningService
{
	/// <summary>Dispara quando o microfone entra ou sai de captura activa (escuta offline).</summary>
	event EventHandler<bool>? ListeningActiveChanged;

	/// <summary>True entre <see cref="StartAsync"/> e <see cref="StopAsync"/> (escuta pedida, mesmo a reconectar o microfone).</summary>
	bool IsListening { get; }

	Task StartAsync(MainViewModel viewModel, CancellationToken cancellationToken = default);

	Task StopAsync();

	/// <summary>Retoma a escuta com o último <see cref="MainViewModel"/> usado em <see cref="StartAsync"/>.</summary>
	Task ResumeAsync();
}
