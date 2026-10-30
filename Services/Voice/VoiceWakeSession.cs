using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SaphMovel.Models;

namespace SaphMovel.Services.Voice;

/// <summary>
/// Parser para «olá sistema» + comando (mesma frase ou wake seguido de outra).
/// A wake usa correspondência flexível — o Vosk raramente devolve a grafia exacta.
/// </summary>
public sealed class VoiceWakeSession
{
	readonly string _wakeNorm;

	/// <summary>Frase configurada sem pontuação decorativa (útil se a wake tiver abreviaturas com «.»).</summary>
	readonly string _wakeNormNoDots;

	bool _armedForFollowUpCommand;

	/// <summary>Depois de «olá sistema» sozinho, o comando seguinte só é aceite durante esta janela.</summary>
	static readonly TimeSpan ArmedWindow = TimeSpan.FromSeconds(8);

	DateTime _armedUtc;

	/// <summary>Evita devolver <see cref="DispatchKind.WakeWithoutMenuCommand"/> várias vezes para a mesma sessão de wake.</summary>
	bool _wakePromptShownForCurrentCycle;

	public VoiceWakeSession(string wakePhrase)
	{
		_wakeNorm = Normalize(wakePhrase);
		_wakeNormNoDots = Normalize(wakePhrase.Replace(".", "", StringComparison.Ordinal));
	}

	public static string Normalize(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return "";
		var sb = new StringBuilder(text.Length);
		foreach (var rune in text.Normalize(NormalizationForm.FormD))
		{
			var uc = CharUnicodeInfo.GetUnicodeCategory(rune);
			if (uc == UnicodeCategory.NonSpacingMark)
				continue;
			sb.Append(rune);
		}
		var s = sb.ToString().Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
		return Regex.Replace(s, @"\s+", " ");
	}

	/// <summary>Resultado da análise de uma linha de texto reconhecido.</summary>
	public enum DispatchKind
	{
		Nothing,
		/// <summary>Wake ouvida mas ainda sem comando de menu — pode dar feedback ao utilizador.</summary>
		WakeWithoutMenuCommand,
		/// <summary>Comando de menu identificado — executar acção.</summary>
		MenuCommand,
	}

	/// <summary>
	/// Avalia o texto acumulado (várias frases finais separadas por espaço).
	/// </summary>
	public DispatchKind TryEvaluate(
		string transcriptRaw,
		IReadOnlyList<(string OriginalTitle, string NormalizedTitle)> menuEntries,
		out string? menuTitleOriginal)
	{
		menuTitleOriginal = null;
		var combined = Normalize(transcriptRaw);
		if (combined.Length == 0)
			return DispatchKind.Nothing;

		if (TryFindWake(combined, out var tailAfterWake))
		{
			if (TryMatchMenuTail(tailAfterWake, menuEntries, out menuTitleOriginal))
			{
				_armedForFollowUpCommand = false;
				_wakePromptShownForCurrentCycle = false;
				return DispatchKind.MenuCommand;
			}
			_armedForFollowUpCommand = true;
			_armedUtc = DateTime.UtcNow;
			if (_wakePromptShownForCurrentCycle)
				return DispatchKind.Nothing;
			_wakePromptShownForCurrentCycle = true;
			return DispatchKind.WakeWithoutMenuCommand;
		}

		if (_armedForFollowUpCommand && DateTime.UtcNow - _armedUtc > ArmedWindow)
		{
			_armedForFollowUpCommand = false;
			_wakePromptShownForCurrentCycle = false;
		}

		if (_armedForFollowUpCommand && TryMatchMenuTail(combined, menuEntries, out menuTitleOriginal))
		{
			_armedForFollowUpCommand = false;
			_wakePromptShownForCurrentCycle = false;
			return DispatchKind.MenuCommand;
		}

		return DispatchKind.Nothing;
	}

	/// <summary>Localiza a wake no texto normalizado e devolve o que vem a seguir.</summary>
	bool TryFindWake(string n, out string tailAfterWake)
	{
		tailAfterWake = "";
		if (string.IsNullOrEmpty(n))
			return false;

		// 1) Frase configurada e variante sem «.» (ex.: «ola.sistema» → «olasistema» não bate; sem pontos volta a «ola sistema»).
		foreach (var variant in new[] { _wakeNorm, _wakeNormNoDots }.Distinct())
		{
			if (variant.Length == 0)
				continue;
			var idx = n.IndexOf(variant, StringComparison.Ordinal);
			if (idx < 0)
				continue;
			tailAfterWake = n[(idx + variant.Length)..].Trim();
			return true;
		}

		// 2) Homófonos / grafias próximas de «olá sistema» (texto já normalizado)
		foreach (var needle in WakeSynonyms.OrderByDescending(x => x.Length))
		{
			var i = n.IndexOf(needle, StringComparison.Ordinal);
			if (i < 0)
				continue;
			tailAfterWake = n[(i + needle.Length)..].Trim();
			return true;
		}

		// 3) Sem espaços (modelo compacta às vezes)
		var compact = n.Replace(" ", "", StringComparison.Ordinal);
		foreach (var token in WakeCompactTokens.OrderByDescending(x => x.Length))
		{
			var ci = compact.IndexOf(token, StringComparison.Ordinal);
			if (ci < 0)
				continue;
			tailAfterWake = "";
			return true;
		}

		// 4) Regex: «ola» ouvido como hola/alo + «sistema» com erros comuns; soletração «o l a»
		var m = WakeMishearRegex.Match(n);
		if (m.Success)
		{
			tailAfterWake = n[(m.Index + m.Length)..].Trim();
			return true;
		}

		m = WakeLooseRegex.Match(n);
		if (m.Success)
		{
			tailAfterWake = n[(m.Index + m.Length)..].Trim();
			return true;
		}

		// 5) Só «sistema» — o ASR muitas vezes perde o «olá» inicial.
		m = WakeSistemaOnlyRegex.Match(n);
		if (!m.Success)
			return false;
		tailAfterWake = n[(m.Index + m.Length)..].Trim();
		return true;
	}

	static readonly string[] WakeSynonyms =
	[
		// Homófonos / confusões frequentes do ASR em PT para «olá sistema» (minúsculas, sem acentos — como o normalizador da transcrição)
		"o la sistema",
		"ou la sistema",
		"hola sistema",
		"alo sistema",
		"ola sistemas",
		"ola sistena",
		"ola sitema",
		"ola cistema",
		"ola systema",
		"ola sistem",
		"ola o sistema",
		"ola si stema",
		"ola sis tema",
		"ola chi stema",
		"ola l sistema",
		"oh la sistema"
	];

	static readonly string[] WakeCompactTokens =
	[
		"olasistema",
		"holasistema",
		"alosistema",
		"olasistena",
		"olasitema",
	];

	static readonly Regex WakeLooseRegex = new(
		@"(^|\s)(ola|hola|alo|ora)\s+(sistema|sistena|sitema|cistema|sistemas|systema)(\s|$)",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	/// <summary>«sistema» isolado (e grafias próximas) como palavra inteira — wake sem o «olá».</summary>
	static readonly Regex WakeSistemaOnlyRegex = new(
		@"(^|\s)(sistema|sistemas|sistena|sitema|cistema|systema)(\s|$)",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	/// <summary>Soletração «o l a» ou «ou la» + sistema; «ola» colada a «sistema» com hífen opcional.</summary>
	static readonly Regex WakeMishearRegex = new(
		@"(^|\s)((o\s+l\s+a)|(ou\s+la))\s+sistema(\s|$)|(^|\s)ola[-\s]?sistema(\s|$)",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	static bool TryMatchMenuTail(
		string tailNorm,
		IReadOnlyList<(string OriginalTitle, string NormalizedTitle)> menuEntries,
		out string? menuTitleOriginal)
	{
		menuTitleOriginal = null;
		if (tailNorm.Length == 0)
			return false;

		if (VoiceStopSpeechMatcher.IsStopCommand(tailNorm))
		{
			foreach (var (original, norm) in menuEntries)
			{
				if (norm.Equals("pare", StringComparison.Ordinal)
				    || original.Equals("Pare", StringComparison.OrdinalIgnoreCase))
				{
					menuTitleOriginal = original;
					return true;
				}
			}

			menuTitleOriginal = "Pare";
			return true;
		}

		if (TryMatchProtocolo(tailNorm, menuEntries, out menuTitleOriginal))
			return true;

		// Frase completa do ítem do menu — evita Contains que apanha substrings dentro da wake ou de outras palavras.
		foreach (var (original, norm) in menuEntries.OrderByDescending(x => x.NormalizedTitle.Length))
		{
			if (norm.Length == 0)
				continue;
			if (!ContainsAsWholePhrase(tailNorm, norm))
				continue;
			menuTitleOriginal = original;
			return true;
		}

		// Modelos às vezes omitiram «solicitar» — palavras-chave únicas por ítem do menu.
		foreach (var kw in DistinctiveMenuKeywords.OrderByDescending(x => x.Length))
		{
			if (!ContainsAsWholePhrase(tailNorm, kw))
				continue;
			foreach (var (original, norm) in menuEntries)
			{
				if (!norm.Contains(kw, StringComparison.Ordinal))
					continue;
				menuTitleOriginal = original;
				return true;
			}
		}

		return false;
	}

	/// <summary>Protocolo dito sem a wake (ex.: «ativar protocolo de afogamento», «afogamento»).</summary>
	public static bool TryMatchProtocoloSemWake(
		string transcriptRaw,
		IReadOnlyList<(string OriginalTitle, string NormalizedTitle)> menuEntries,
		out string? menuTitleOriginal)
	{
		menuTitleOriginal = null;
		var n = Normalize(transcriptRaw);
		return n.Length > 0 && TryMatchProtocolo(n, menuEntries, out menuTitleOriginal);
	}

	static bool TryMatchProtocolo(
		string tailNorm,
		IReadOnlyList<(string OriginalTitle, string NormalizedTitle)> menuEntries,
		out string? menuTitleOriginal)
	{
		menuTitleOriginal = null;
		ProtocoloAtendimentoDefinicao? matched = null;
		var bestLen = 0;
		foreach (var def in ProtocoloAtendimentoCatalog.All)
		{
			foreach (var syn in def.VoiceSynonyms)
			{
				var synNorm = Normalize(syn);
				if (synNorm.Length == 0 || synNorm.Length <= bestLen)
					continue;
				if (!ContainsAsWholePhrase(tailNorm, synNorm) && !tailNorm.Contains(synNorm, StringComparison.Ordinal))
					continue;
				matched = def;
				bestLen = synNorm.Length;
			}
		}

		if (matched is null)
			return false;

		var commandNorm = Normalize(matched.VoiceCommandTitle);
		foreach (var (original, norm) in menuEntries)
		{
			if (!string.Equals(norm, commandNorm, StringComparison.Ordinal)
			    && !norm.Contains(commandNorm, StringComparison.Ordinal)
			    && !string.Equals(original, matched.VoiceCommandTitle, StringComparison.OrdinalIgnoreCase))
				continue;
			menuTitleOriginal = original;
			return true;
		}

		menuTitleOriginal = matched.VoiceCommandTitle;
		return true;
	}

	/// <summary><paramref name="needleNorm"/> como subsequência delimitada por espaços / início / fim — não faz match dentro de outra palavra.</summary>
	internal static bool ContainsAsWholePhrase(string haystackNorm, string needleNorm)
	{
		if (needleNorm.Length == 0 || haystackNorm.Length < needleNorm.Length)
			return false;
		var idx = 0;
		while ((idx = haystackNorm.IndexOf(needleNorm, idx, StringComparison.Ordinal)) >= 0)
		{
			var beforeOk = idx == 0 || char.IsWhiteSpace(haystackNorm[idx - 1]);
			var after = idx + needleNorm.Length;
			var afterOk = after >= haystackNorm.Length || char.IsWhiteSpace(haystackNorm[after]);
			if (beforeOk && afterOk)
				return true;
			idx++;
		}

		return false;
	}

	static readonly string[] DistinctiveMenuKeywords =
	[
		"abastecimento",
		"higienizacao",
		"manutencao",
		"atendimento",
		"atualizar endereco",
		"parada cardiorrespiratoria",
		"cardiorrespiratoria",
		"afogamento",
		"atropelamento",
		"para de falar",
		"pare de falar",
	];
}
