namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Identificação (Boletim APH).</summary>
public sealed class IdentificacaoFormData : ObservableModel
{
	string _nome = string.Empty;
	string _sexo = string.Empty;
	string _idadeAnos = string.Empty;
	string _idadeMeses = string.Empty;
	string _cpf = string.Empty;
	string _cor = string.Empty;
	string _caracteristicaFisica = string.Empty;
	string _nomeSocial = string.Empty;
	string _nomeMae = string.Empty;
	string _dataNascimentoText = string.Empty;
	string _alergias = string.Empty;

	public string Nome
	{
		get => _nome;
		set => SetField(ref _nome, value);
	}

	/// <summary>"F" feminino, "M" masculino, vazio se não escolhido.</summary>
	public string Sexo
	{
		get => _sexo;
		set => SetField(ref _sexo, value);
	}

	public string IdadeAnos
	{
		get => _idadeAnos;
		set => SetField(ref _idadeAnos, value);
	}

	public string IdadeMeses
	{
		get => _idadeMeses;
		set => SetField(ref _idadeMeses, value);
	}

	public string Cpf
	{
		get => _cpf;
		set => SetField(ref _cpf, value);
	}

	public string Cor
	{
		get => _cor;
		set => SetField(ref _cor, value);
	}

	public string CaracteristicaFisica
	{
		get => _caracteristicaFisica;
		set => SetField(ref _caracteristicaFisica, value);
	}

	public string NomeSocial
	{
		get => _nomeSocial;
		set => SetField(ref _nomeSocial, value);
	}

	public string NomeMae
	{
		get => _nomeMae;
		set => SetField(ref _nomeMae, value);
	}

	public string DataNascimentoText
	{
		get => _dataNascimentoText;
		set => SetField(ref _dataNascimentoText, value);
	}

	public string Alergias
	{
		get => _alergias;
		set => SetField(ref _alergias, value);
	}

	/// <summary>Limpa o formulário (novo atendimento / desvio de missão).</summary>
	public void Reset()
	{
		Nome = string.Empty;
		Sexo = string.Empty;
		IdadeAnos = string.Empty;
		IdadeMeses = string.Empty;
		Cpf = string.Empty;
		Cor = string.Empty;
		CaracteristicaFisica = string.Empty;
		NomeSocial = string.Empty;
		NomeMae = string.Empty;
		DataNascimentoText = string.Empty;
		Alergias = string.Empty;
	}

}
