namespace SaphEventos;

/// <summary>Payload de demonstração para o simulador local.</summary>
public static class DemoChamadoFactory
{
	public static ChamadoPayload Create()
	{
		var p = new ChamadoPayload
		{
			NumeroOcorrencia = "333604",
			Referencia = "TRUE TECNOLOGIA",
			Acionamento = "Código 1",
			OrientacaoRegulacao = "Dor 8/10, sem desconforto respiratório, sem medicação em domicílio.",
			ApoioPor = "--",
			TipoCliente = "Particular - PJ",
			CodigoCliente = "137150",
			DescricaoEvento = "Deslocar",
			DataHoraEvento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
			Latitude = -30.0210,
			Longitude = -51.1933,
			NomeEquipe = "SA02 CRISTO",
			Logradouro = "Avenida Montenegro",
			Cidade = "Porto Alegre",
			Bairro = "Petrópolis",
			Numero = "145",
			NomePaciente = "Milton Petry",
			Idade = "42",
			Sexo = "M",
			TipoDeSocorro = "atendimento clínico por síndrome neurológica",
		};
		p.EnsureDisplayFields();
		return p;
	}

	/// <summary>Chamado alternativo para demo (endereço/paciente diferentes).</summary>
	public static ChamadoPayload CreateAlternativo()
	{
		var p = new ChamadoPayload
		{
			NumeroOcorrencia = "441902",
			Referencia = "Próximo ao mercado",
			Acionamento = "Código 2",
			OrientacaoRegulacao = "Dispneia leve, consciente e orientado.",
			ApoioPor = "--",
			TipoCliente = "SUS",
			CodigoCliente = "220011",
			DescricaoEvento = "Deslocar",
			DataHoraEvento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
			Latitude = -30.0342,
			Longitude = -51.2178,
			NomeEquipe = "SA02 CRISTO",
			Logradouro = "Rua João Alfredo",
			Cidade = "Porto Alegre",
			Bairro = "Cidade Baixa",
			Numero = "512",
			NomePaciente = "Ana Souza",
			Idade = "67",
			Sexo = "F",
			TipoDeSocorro = "afogamento em banheira",
		};
		p.EnsureDisplayFields();
		return p;
	}

	/// <summary>Payload para simular desvio de missão (prioridade).</summary>
	public static ChamadoPayload CreateDesvio()
	{
		var p = new ChamadoPayload
		{
			NumeroOcorrencia = "998877",
			Referencia = "Cruzamento com sinal",
			Acionamento = "Código 1",
			OrientacaoRegulacao = "Atropelamento, vítima consciente, dor torácica.",
			ApoioPor = "Bombeiros",
			TipoCliente = "SUS",
			CodigoCliente = "330044",
			DescricaoEvento = "Desvio de missão",
			DataHoraEvento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
			Latitude = -30.0123,
			Longitude = -51.2055,
			NomeEquipe = "SA02 CRISTO",
			Logradouro = "Avenida Ipiranga",
			Cidade = "Porto Alegre",
			Bairro = "Azenha",
			Numero = "1200",
			NomePaciente = "Carlos Menezes",
			Idade = "34",
			Sexo = "M",
			TipoDeSocorro = "atropelamento",
		};
		p.EnsureDisplayFields();
		return p;
	}
}
