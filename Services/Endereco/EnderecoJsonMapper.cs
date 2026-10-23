using System.Text.Json;
using SaphMovel.Models;
using SaphMovel.Services.Json;

namespace SaphMovel.Services.Endereco;

/// <summary>Mapeia JSON de endereço para <see cref="EnderecoFormData"/>.</summary>
public static class EnderecoJsonMapper
{
	public static bool TryApply(EnderecoFormData form, string? json, out string? error)
	{
		error = null;
		if (form is null)
		{
			error = "Formulário inválido.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(json))
		{
			error = "JSON vazio.";
			return false;
		}

		EnderecoPayload payload;
		try
		{
			payload = ParsePayload(json);
		}
		catch (JsonException)
		{
			error = "JSON inválido. Use o formato {Logradouro: \"...\", Numero: \"...\", ...}.";
			return false;
		}

		if (!PayloadHasAnyValue(payload))
		{
			error = "JSON sem dados reconhecidos (Logradouro, Numero, Bairro, Cidade, Referencia).";
			return false;
		}

		ApplyPayload(form, payload);
		return true;
	}

	public static void ApplyPayload(EnderecoFormData form, EnderecoPayload payload)
	{
		if (!string.IsNullOrWhiteSpace(payload.Logradouro))
			form.Logradouro = payload.Logradouro.Trim();
		if (!string.IsNullOrWhiteSpace(payload.Numero))
			form.Numero = payload.Numero.Trim();
		if (!string.IsNullOrWhiteSpace(payload.Bairro))
			form.Bairro = payload.Bairro.Trim();
		if (!string.IsNullOrWhiteSpace(payload.Cidade))
			form.Cidade = payload.Cidade.Trim();
		if (!string.IsNullOrWhiteSpace(payload.Referencia))
			form.Referencia = payload.Referencia.Trim();
	}

	internal static EnderecoPayload ParsePayload(string raw)
	{
		var normalized = LenientJson.NormalizeToStandardJson(raw);
		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;
		if (root.ValueKind != JsonValueKind.Object)
			throw new JsonException("Raiz JSON deve ser um objeto.");

		var payload = new EnderecoPayload();
		foreach (var prop in root.EnumerateObject())
		{
			if (!EnderecoKeyAliases.TryResolveCanonical(prop.Name, out var canonical))
				continue;

			var text = ReadString(prop.Value);
			if (string.IsNullOrWhiteSpace(text))
				continue;

			switch (canonical)
			{
				case EnderecoKeyAliases.Logradouro:
					payload.Logradouro = text;
					break;
				case EnderecoKeyAliases.Numero:
					payload.Numero = text;
					break;
				case EnderecoKeyAliases.Bairro:
					payload.Bairro = text;
					break;
				case EnderecoKeyAliases.Cidade:
					payload.Cidade = text;
					break;
				case EnderecoKeyAliases.Referencia:
					payload.Referencia = text;
					break;
			}
		}

		return payload;
	}

	static string? ReadString(JsonElement el) => el.ValueKind switch
	{
		JsonValueKind.Null or JsonValueKind.Undefined => null,
		JsonValueKind.String => el.GetString(),
		JsonValueKind.Number => el.GetRawText(),
		_ => el.ToString(),
	};

	static bool PayloadHasAnyValue(EnderecoPayload p) =>
		!string.IsNullOrWhiteSpace(p.Logradouro)
		|| !string.IsNullOrWhiteSpace(p.Numero)
		|| !string.IsNullOrWhiteSpace(p.Bairro)
		|| !string.IsNullOrWhiteSpace(p.Cidade)
		|| !string.IsNullOrWhiteSpace(p.Referencia);
}
