using System.Text.Json.Serialization;

namespace SaphMovel.Services.Endereco;

/// <summary>Payload de endereço extraído por voz / JSON.</summary>
public sealed class EnderecoPayload
{
	[JsonPropertyName("Logradouro")]
	public string? Logradouro { get; set; }

	[JsonPropertyName("Numero")]
	public string? Numero { get; set; }

	[JsonPropertyName("Bairro")]
	public string? Bairro { get; set; }

	[JsonPropertyName("Cidade")]
	public string? Cidade { get; set; }

	[JsonPropertyName("Referencia")]
	public string? Referencia { get; set; }
}
