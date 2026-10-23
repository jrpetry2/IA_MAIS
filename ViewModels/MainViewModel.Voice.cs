using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SaphMovel.Configuration;
using SaphMovel.Models;
using SaphMovel.Services.Voice;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel
{
	public const string VoiceCommandAtualizarEndereco = "Atualizar endereço";
	public const string VoiceCommandProtocoloPcr = "Parada cardiorrespiratória";
	public const string VoiceCommandProtocoloAfogamento = "Afogamento";
	public const string VoiceCommandProtocoloAtropelamento = "Atropelamento";
	public const string VoiceCommandPare = "Pare";

	DateTime _lastVoiceWakeHintUtc;

	/// <summary>Microfone activo para escuta offline (Vosk).</summary>
	public bool IsVoiceMicrophoneActive
	{
		get => _isVoiceMicrophoneActive;
		set
		{
			if (!SetField(ref _isVoiceMicrophoneActive, value))
				return;
			if (value && _voiceModelReadyHint is not null)
			{
				_voiceModelReadyHint = null;
				OnPropertyChanged(nameof(VoiceModelReadyHint));
			}
			OnPropertyChanged(nameof(ShowVoiceModelReadyHint));
			OnPropertyChanged(nameof(ShowVoiceStatusArea));
			OnPropertyChanged(nameof(VoiceStatusBannerBackgroundColor));
		}
	}

	bool _isVoiceMicrophoneActive;

	/// <summary>Mensagem quando a voz offline não está a ouvir (modelo em falta, permissão, etc.).</summary>
	public string? VoiceOfflineHint => _voiceOfflineHint;

	string? _voiceOfflineHint;

	public bool ShowVoiceOfflineHint => !string.IsNullOrWhiteSpace(_voiceOfflineHint);

	/// <summary>Entre modelo OK no disco e microfone a gravar — evita faixa «em branco».</summary>
	public string? VoiceModelReadyHint => _voiceModelReadyHint;

	string? _voiceModelReadyHint;

	public bool ShowVoiceModelReadyHint =>
		!string.IsNullOrWhiteSpace(_voiceModelReadyHint) && !IsVoiceMicrophoneActive && !ShowVoiceOfflineHint;

	/// <summary>Mostra a faixa de estado de voz (modelo pronto, a ouvir ou aviso).</summary>
	public bool ShowVoiceStatusArea =>
		IsVoiceMicrophoneActive || ShowVoiceOfflineHint || ShowVoiceModelReadyHint;

	public Color VoiceStatusBannerBackgroundColor =>
		IsVoiceMicrophoneActive
			? ResourceColor("SaphVoiceActiveTint", "#1A2DBE1B")
			: ShowVoiceModelReadyHint && !ShowVoiceOfflineHint
				? ResourceColor("SaphVoiceInfoTint", "#331996F6")
				: ResourceColor("SaphVoiceWarnTint", "#33FFB020");

	string? _lastVoiceHeardText;
	int _successToastGeneration;

	/// <summary>Última frase final reconhecida pelo Vosk (para validar ASR na UI).</summary>
	public string? LastVoiceHeardText => _lastVoiceHeardText;

	public bool ShowLastVoiceHeard =>
		IsVoiceMicrophoneActive && !string.IsNullOrWhiteSpace(_lastVoiceHeardText);

	public string VoiceListeningStatusText => ShowLastVoiceHeard
		? $"Ouvi: «{_lastVoiceHeardText}»"
		: "A ouvir — diga «olá sistema» e um protocolo, ou «pare» para interromper a fala.";

	/// <summary>True enquanto o assistente está a falar (TTS).</summary>
	public bool IsAssistantSpeaking => _chamadoSpeech.IsSpeaking;

	/// <summary>Fim (UTC) da última fala do assistente — frases ouvidas logo a seguir são tratadas como eco.</summary>
	public DateTime AssistantSpeechEndedUtc => _chamadoSpeech.LastSpeechEndedUtc;

	/// <summary>
	/// Durante o TTS, aceita «pare» / similares sem wake e cancela a fala.
	/// </summary>
	public bool TryHandleStopSpeechUtterance(string utterance)
	{
		if (!_chamadoSpeech.IsSpeaking)
			return false;
		if (!VoiceStopSpeechMatcher.IsStopCommand(utterance))
			return false;
		CancelAssistantSpeech();
		return true;
	}

	public void CancelAssistantSpeech() => _chamadoSpeech.Cancel();

	bool _showSuccessToast;
	string _successToastText = "";

	public bool ShowSuccessToast => _showSuccessToast;
	public string SuccessToastText => _successToastText;

	public void SetLastVoiceHeardText(string? text)
	{
		var v = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
		if (string.Equals(_lastVoiceHeardText, v, StringComparison.Ordinal))
			return;
		_lastVoiceHeardText = v;
		OnPropertyChanged(nameof(LastVoiceHeardText));
		OnPropertyChanged(nameof(ShowLastVoiceHeard));
		OnPropertyChanged(nameof(VoiceListeningStatusText));
	}

	public async Task ShowSuccessToastAsync(string message, int visibleMs = 4500)
	{
		var gen = Interlocked.Increment(ref _successToastGeneration);
		var text = message?.Trim() ?? "";

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			_successToastText = text;
			_showSuccessToast = text.Length > 0;
			OnPropertyChanged(nameof(SuccessToastText));
			OnPropertyChanged(nameof(ShowSuccessToast));
		}).ConfigureAwait(false);

		try
		{
			await Task.Delay(Math.Max(1500, visibleMs)).ConfigureAwait(false);
		}
		catch
		{
			return;
		}

		if (gen != _successToastGeneration)
			return;

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (gen != _successToastGeneration)
				return;
			_showSuccessToast = false;
			OnPropertyChanged(nameof(ShowSuccessToast));
		}).ConfigureAwait(false);
	}

	public void SetVoiceOfflineHint(string? message)
	{
		var v = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
		if (_voiceOfflineHint == v)
			return;
		_voiceOfflineHint = v;
		if (v is not null)
			_voiceModelReadyHint = null;
		OnPropertyChanged(nameof(VoiceOfflineHint));
		OnPropertyChanged(nameof(VoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceOfflineHint));
		OnPropertyChanged(nameof(ShowVoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceStatusArea));
		OnPropertyChanged(nameof(VoiceStatusBannerBackgroundColor));
	}

	public void SetVoiceModelReadyHint(string? message)
	{
		var v = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
		if (_voiceModelReadyHint == v)
			return;
		_voiceModelReadyHint = v;
		OnPropertyChanged(nameof(VoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceModelReadyHint));
		OnPropertyChanged(nameof(ShowVoiceStatusArea));
		OnPropertyChanged(nameof(VoiceStatusBannerBackgroundColor));
	}

	public IReadOnlyList<(string OriginalTitle, string NormalizedTitle)> GetVoiceCommandEntries()
	{
		var list = new List<(string, string)>(MenuItems.Count + 3 + ProtocoloAtendimentoCatalog.All.Count);
		foreach (var m in MenuItems)
			list.Add((m.Title, VoiceWakeSession.Normalize(m.Title)));
		list.Add((VoiceCommandAtualizarEndereco, VoiceWakeSession.Normalize(VoiceCommandAtualizarEndereco)));
		list.Add((VoiceCommandPare, VoiceWakeSession.Normalize(VoiceCommandPare)));
		foreach (var p in ProtocoloAtendimentoCatalog.All)
			list.Add((p.VoiceCommandTitle, VoiceWakeSession.Normalize(p.VoiceCommandTitle)));
		return list;
	}

	/// <summary>
	/// Invoca o mesmo fluxo do toque na linha do menu STATUS (comando de voz).
	/// </summary>
	public bool TryInvokeMenuItemFromVoiceCaption(string menuTitleOriginal)
	{
		if (string.Equals(
			    menuTitleOriginal.Trim(),
			    VoiceCommandAtualizarEndereco,
			    StringComparison.OrdinalIgnoreCase))
		{
			_ = OpenAlterarEnderecoAsync();
			return true;
		}

		if (string.Equals(menuTitleOriginal.Trim(), VoiceCommandPare, StringComparison.OrdinalIgnoreCase)
		    || VoiceStopSpeechMatcher.IsStopCommand(menuTitleOriginal))
		{
			CancelAssistantSpeech();
			return true;
		}

		if (ProtocoloAtendimentoCatalog.TryGetByVoiceCommandTitle(menuTitleOriginal, out var protocolo)
		    && protocolo is not null)
		{
			AtivarProtocolo(protocolo.Id);
			return true;
		}

		foreach (var item in MenuItems)
		{
			if (!string.Equals(item.Title.Trim(), menuTitleOriginal.Trim(), StringComparison.OrdinalIgnoreCase))
				continue;
			_ = ShowStatusMenuItemClickPlaceholderAsync(item);
			return true;
		}
		return false;
	}

	/// <summary>Feedback quando a wake foi ouvida mas falta o comando do menu (debounced).</summary>
	public void NotifyVoiceWakeWithoutCommandDebounced()
	{
		var now = DateTime.UtcNow;
		if (now - _lastVoiceWakeHintUtc < TimeSpan.FromSeconds(14))
			return;
		_lastVoiceWakeHintUtc = now;
		_ = ShowVoiceWakeFollowUpHintAsync();
	}

	static async Task ShowVoiceWakeFollowUpHintAsync()
	{
		await ShowAlertFromAnyThreadAsync(
			"Voz",
			$"«{VoskVoiceConfig.WakePhrase}» foi reconhecido. Diga em seguida o comando, por exemplo: Afogamento ou Parada cardíaca.")
			.ConfigureAwait(false);
	}
}
