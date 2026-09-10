namespace SaphMovel.Services.Audio;



/// <summary>Caminhos de áudio da tela Sinais Vitais.</summary>

public static class SinaisVitaisAudioPaths

{

	/// <summary>Ficheiro gravado (AAC em contentor MP4 — aceite pela API OpenAI como <c>audio/mp4</c>).</summary>

	public const string RecordingFileName = "audio.m4a";



	/// <summary>Raiz gravável da app (<see cref="FileSystem.AppDataDirectory"/>).</summary>

	public static string AppRootRecordingFile =>

		Path.Combine(FileSystem.AppDataDirectory, RecordingFileName);

}

