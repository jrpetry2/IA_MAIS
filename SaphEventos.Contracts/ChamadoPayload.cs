namespace SaphEventos;

/// <summary>Dados básicos do chamado exibidos na aba CHAMADO e usados na leitura por voz.</summary>
public sealed class ChamadoPayload
{
	public string NumeroOcorrencia { get; set; } = "";
	public string PacienteLinha { get; set; } = "";
	public string Endereco { get; set; } = "";
	public string Referencia { get; set; } = "";
	public string Acionamento { get; set; } = "";
	public string Atendimento { get; set; } = "";
	public string OrientacaoRegulacao { get; set; } = "";
	public string ApoioPor { get; set; } = "";
	public string TipoCliente { get; set; } = "";
	public string CodigoCliente { get; set; } = "";
	public string DescricaoEvento { get; set; } = "";
	public string DataHoraEvento { get; set; } = "";
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }

	/// <summary>Nome da equipe para a fala (ex.: SA02 CRISTO).</summary>
	public string NomeEquipe { get; set; } = "";

	public string Logradouro { get; set; } = "";
	public string Cidade { get; set; } = "";
	public string Bairro { get; set; } = "";
	public string Numero { get; set; } = "";

	public string NomePaciente { get; set; } = "";
	public string Idade { get; set; } = "";
	/// <summary>M, F ou vazio.</summary>
	public string Sexo { get; set; } = "";

	public string TipoDeSocorro { get; set; } = "";

	/// <summary>Preenche <see cref="Endereco"/> e <see cref="PacienteLinha"/> a partir dos campos estruturados, se estiverem vazios.</summary>
	public void EnsureDisplayFields()
	{
		if (string.IsNullOrWhiteSpace(Endereco)
		    && (!string.IsNullOrWhiteSpace(Logradouro) || !string.IsNullOrWhiteSpace(Cidade)))
		{
			var num = string.IsNullOrWhiteSpace(Numero) ? "" : $", {Numero.Trim()}";
			var bairro = string.IsNullOrWhiteSpace(Bairro) ? "" : $", {Bairro.Trim()}";
			var cidade = string.IsNullOrWhiteSpace(Cidade) ? "" : $" - {Cidade.Trim()}";
			Endereco = $"{Logradouro.Trim()}{num}{bairro}{cidade}".Trim(' ', ',');
		}

		if (string.IsNullOrWhiteSpace(PacienteLinha) && !string.IsNullOrWhiteSpace(NomePaciente))
		{
			var id = string.IsNullOrWhiteSpace(Idade) ? "" : Idade.Trim();
			var sex = string.IsNullOrWhiteSpace(Sexo) ? "" : Sexo.Trim().ToUpperInvariant();
			if (sex.Length > 1)
				sex = sex[..1];
			PacienteLinha = string.IsNullOrEmpty(id) && string.IsNullOrEmpty(sex)
				? NomePaciente.Trim()
				: $"{NomePaciente.Trim()},{id} {sex}".Trim();
		}

		if (string.IsNullOrWhiteSpace(Atendimento) && !string.IsNullOrWhiteSpace(TipoDeSocorro))
			Atendimento = TipoDeSocorro.Trim();
	}
}
