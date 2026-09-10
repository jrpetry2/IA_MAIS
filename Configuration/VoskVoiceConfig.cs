namespace SaphMovel.Configuration;

/// <summary>Constantes para Vosk offline (Android).</summary>
public static class VoskVoiceConfig
{
	/// <summary>Frase de activação (normalizada no parser).</summary>
	public const string WakePhrase = "olá sistema";

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
