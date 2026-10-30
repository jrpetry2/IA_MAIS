namespace SaphMovel.Configuration;

/// <summary>Constantes para Vosk offline (Android).</summary>
public static class VoskVoiceConfig
{
	/// <summary>Frase de activação (normalizada no parser).</summary>
	public const string WakePhrase = "olá sistema";

	/// <summary>
	/// Restringe o Vosk à gramática de comandos (<c>VoiceCommandGrammar</c>) — muito mais preciso para protocolos e sim/não.
	/// Desligue para voltar ao reconhecimento de texto livre.
	/// </summary>
	public const bool UseCommandGrammar = true;

	/// <summary>Liga os filtros de confiança e de volume (descarta conversa de quem está ao redor).</summary>
	public static readonly bool UtteranceFilterEnabled = true;

	/// <summary>Confiança média mínima das palavras (0–1, sem «[unk]») para aceitar a frase.</summary>
	public const double MinWordConfidence = 0.6;

	/// <summary>Volume mínimo da frase (pico de RMS em dBFS). Vozes a 2–3 m chegam bem mais baixas — calibrar no aparelho.</summary>
	public const double MinUtteranceLevelDbfs = -42.0;

	/// <summary>Mostra na faixa «Ouvi» as frases descartadas pelo filtro (para calibrar). Desligar para a apresentação.</summary>
	public static readonly bool ShowFilteredInBanner = true;

	/// <summary>
	/// Fonte do microfone da escuta contínua. <c>VoiceCommunication</c> activa, em muitos aparelhos, vários microfones e
	/// supressão para voz próxima, mas pode alterar o volume/saída do TTS — testar antes de adoptar.
	/// </summary>
	public static readonly global::Android.Media.AudioSource MicSource = global::Android.Media.AudioSource.VoiceRecognition;

	/// <summary>Frases finais que chegam logo depois do fim do TTS são quase sempre eco da própria fala.</summary>
	public static readonly TimeSpan IgnoreAfterSpeechEnd = TimeSpan.FromMilliseconds(700);

	/// <summary>Durante o atendimento, aceita protocolos sem «olá sistema» (ex.: «afogamento»).</summary>
	public static readonly bool ProtocoloSemWakeEmAtendimento = true;

	/// <summary>
	/// Protocolo sem wake só depois desta folga após o TTS — a própria fala diz «protocolo afogamento» e o eco
	/// voltaria a activá-lo.
	/// </summary>
	public static readonly TimeSpan ProtocoloSemWakeIgnoreAfterSpeechEnd = TimeSpan.FromSeconds(2);

	/// <summary>Nome lógico do ZIP empacotado (MauiAsset em <c>Resources/Raw</c>).</summary>
	public const string PackagedModelZipLogicalName = "vosk-model-small-pt.zip";

	/// <summary>
	/// Tentativas <see cref="FileSystem.OpenAppPackageFileAsync"/> — o nome no APK deve coincidir com o <c>LogicalName</c> do <c>MauiAsset</c>.
	/// O site pode distribuir «vosk-model-small-pt-0.zip»; dentro há uma pasta tipo «vosk-model-small-pt-0.3» com o modelo.
	/// </summary>
	public static readonly string[] PackagedModelZipCandidateNames =
	[
		PackagedModelZipLogicalName,
		"vosk-model-small-pt-0.zip",
	];

	/// <summary>Pasta sob <see cref="FileSystem.AppDataDirectory"/> com o modelo extraído.</summary>
	public const string ExtractedModelDirName = "vosk-model-pt-extracted";
}
