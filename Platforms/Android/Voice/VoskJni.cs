using Android.Runtime;
using Java.Lang;

namespace SaphMovel.Platforms.Android.Voice;

/// <summary>
/// org.vosk.Model / Recognizer via JNI (<see cref="JNIEnv"/>). Evita <c>Android.Runtime.AndroidJavaObject</c>,
/// ausente do assembly de referência Mono.Android em .NET 10.
/// </summary>
static class VoskJni
{
	static readonly object Gate = new();
	static bool _ready;

	static IntPtr _modelClass = IntPtr.Zero;
	static IntPtr _recognizerClass = IntPtr.Zero;
	static IntPtr _ctorModel = IntPtr.Zero;
	static IntPtr _ctorRecognizer = IntPtr.Zero;
	static IntPtr _acceptWaveForm = IntPtr.Zero;
	static IntPtr _getResult = IntPtr.Zero;
	static IntPtr _ctorRecognizerGrammar = IntPtr.Zero;
	static IntPtr _reset = IntPtr.Zero;
	static IntPtr _setWords = IntPtr.Zero;

	internal static void EnsureReady()
	{
		lock (Gate)
		{
			if (_ready)
				return;

			// Class.ForName(name) sozinho usa o boot class loader nas threads em pool → ClassNotFoundException.
			// É obrigatório passar o ClassLoader do APK (mesmo das classes da app / AARs Maven).
			var appLoader = global::Android.App.Application.Context?.ClassLoader
				?? throw new InvalidOperationException("Application.Context indisponível para ClassLoader.");
			using var modelJvm = Class.ForName("org.vosk.Model", initialize: true, appLoader);
			using var recognizerJvm = Class.ForName("org.vosk.Recognizer", initialize: true, appLoader);

			_modelClass = JNIEnv.NewGlobalRef(modelJvm.Handle);
			_recognizerClass = JNIEnv.NewGlobalRef(recognizerJvm.Handle);

			_ctorModel = JNIEnv.GetMethodID(_modelClass, "<init>", "(Ljava/lang/String;)V");
			_ctorRecognizer = JNIEnv.GetMethodID(_recognizerClass, "<init>", "(Lorg/vosk/Model;F)V");
			_acceptWaveForm = JNIEnv.GetMethodID(_recognizerClass, "acceptWaveForm", "([SI)Z");
			_getResult = JNIEnv.GetMethodID(_recognizerClass, "getResult", "()Ljava/lang/String;");
			_ctorRecognizerGrammar = JNIEnv.GetMethodID(_recognizerClass, "<init>", "(Lorg/vosk/Model;FLjava/lang/String;)V");
			_reset = JNIEnv.GetMethodID(_recognizerClass, "reset", "()V");
			_setWords = JNIEnv.GetMethodID(_recognizerClass, "setWords", "(Z)V");

			_ready = true;
		}
	}

	public static IntPtr CreateModel(string modelDir)
	{
		EnsureReady();
		IntPtr jPath = JNIEnv.NewString(modelDir);
		try
		{
			IntPtr local = JNIEnv.NewObject(_modelClass, _ctorModel, new JValue(jPath));
			IntPtr g = JNIEnv.NewGlobalRef(local);
			JNIEnv.DeleteLocalRef(local);
			return g;
		}
		finally
		{
			JNIEnv.DeleteLocalRef(jPath);
		}
	}

	public static IntPtr CreateRecognizer(IntPtr modelGlobal, float sampleRate)
	{
		EnsureReady();
		IntPtr local = JNIEnv.NewObject(
			_recognizerClass,
			_ctorRecognizer,
			new JValue(modelGlobal),
			new JValue(sampleRate));
		IntPtr g = JNIEnv.NewGlobalRef(local);
		JNIEnv.DeleteLocalRef(local);
		return g;
	}

	/// <summary>Reconhecedor restrito a uma gramática JSON (array de frases, com «[unk]»).</summary>
	public static IntPtr CreateRecognizer(IntPtr modelGlobal, float sampleRate, string grammarJson)
	{
		EnsureReady();
		IntPtr jGrammar = JNIEnv.NewString(grammarJson);
		try
		{
			IntPtr local = JNIEnv.NewObject(
				_recognizerClass,
				_ctorRecognizerGrammar,
				new JValue(modelGlobal),
				new JValue(sampleRate),
				new JValue(jGrammar));
			IntPtr g = JNIEnv.NewGlobalRef(local);
			JNIEnv.DeleteLocalRef(local);
			return g;
		}
		finally
		{
			JNIEnv.DeleteLocalRef(jGrammar);
		}
	}

	/// <summary>Descarta o áudio acumulado (ex.: eco do TTS) e recomeça a frase.</summary>
	public static void Reset(IntPtr recognizerGlobal)
	{
		EnsureReady();
		JNIEnv.CallVoidMethod(recognizerGlobal, _reset);
	}

	/// <summary>Inclui no resultado a lista de palavras com confiança («result»: [{word, conf, …}]).</summary>
	public static void SetWords(IntPtr recognizerGlobal, bool words)
	{
		EnsureReady();
		JNIEnv.CallVoidMethod(recognizerGlobal, _setWords, new JValue(words));
	}

	public static bool AcceptWaveForm(IntPtr recognizerGlobal, short[] pcmSlice)
	{
		EnsureReady();
		IntPtr jarr = JNIEnv.NewArray(pcmSlice);
		try
		{
			var args = new JValue[] { new JValue(jarr), new JValue(pcmSlice.Length) };
			return JNIEnv.CallBooleanMethod(recognizerGlobal, _acceptWaveForm, args);
		}
		finally
		{
			JNIEnv.DeleteLocalRef(jarr);
		}
	}

	public static string GetResult(IntPtr recognizerGlobal)
	{
		EnsureReady();
		IntPtr jstr = JNIEnv.CallObjectMethod(recognizerGlobal, _getResult);
		if (jstr == IntPtr.Zero)
			return "";
		try
		{
			return JNIEnv.GetString(jstr, JniHandleOwnership.DoNotTransfer)!;
		}
		finally
		{
			JNIEnv.DeleteLocalRef(jstr);
		}
	}

	public static void DeleteGlobal(IntPtr globalRef)
	{
		if (globalRef == IntPtr.Zero)
			return;
		try
		{
			JNIEnv.DeleteGlobalRef(globalRef);
		}
		catch
		{
			/* ignore */
		}
	}
}
