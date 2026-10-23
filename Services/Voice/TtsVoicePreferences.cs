using Microsoft.Maui.Storage;

namespace SaphMovel.Services.Voice;

/// <summary>Persistência da voz TTS escolhida pelo utilizador.</summary>
public static class TtsVoicePreferences
{
	const string KeyId = "saph_tts_locale_id";
	const string KeyLanguage = "saph_tts_locale_lang";
	const string KeyCountry = "saph_tts_locale_country";
	const string KeyName = "saph_tts_locale_name";

	public static bool HasSelection => !string.IsNullOrWhiteSpace(Preferences.Get(KeyId, null))
	                                  || !string.IsNullOrWhiteSpace(Preferences.Get(KeyLanguage, null));

	public static void Save(string? id, string? language, string? country, string? name)
	{
		SetOrRemove(KeyId, id);
		SetOrRemove(KeyLanguage, language);
		SetOrRemove(KeyCountry, country);
		SetOrRemove(KeyName, name);
	}

	public static void Clear()
	{
		Preferences.Remove(KeyId);
		Preferences.Remove(KeyLanguage);
		Preferences.Remove(KeyCountry);
		Preferences.Remove(KeyName);
	}

	public static (string? Id, string? Language, string? Country, string? Name) Load() =>
	(
		Preferences.Get(KeyId, null),
		Preferences.Get(KeyLanguage, null),
		Preferences.Get(KeyCountry, null),
		Preferences.Get(KeyName, null)
	);

	public static string DisplayLabel()
	{
		var (_, lang, country, name) = Load();
		if (!string.IsNullOrWhiteSpace(name))
			return name!;
		if (!string.IsNullOrWhiteSpace(lang))
		{
			var c = string.IsNullOrWhiteSpace(country) ? "" : $"-{country}";
			return $"{lang}{c}";
		}

		return "Automática (PT-BR)";
	}

	static void SetOrRemove(string key, string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			Preferences.Remove(key);
		else
			Preferences.Set(key, value);
	}
}
