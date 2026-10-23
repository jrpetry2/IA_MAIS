namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Alterar Endereço.</summary>
public sealed class EnderecoFormData : ObservableModel
{
	string _logradouro = string.Empty;
	string _numero = string.Empty;
	string _bairro = string.Empty;
	string _cidade = string.Empty;
	string _referencia = string.Empty;

	public string Logradouro
	{
		get => _logradouro;
		set => SetField(ref _logradouro, value ?? string.Empty);
	}

	public string Numero
	{
		get => _numero;
		set => SetField(ref _numero, value ?? string.Empty);
	}

	public string Bairro
	{
		get => _bairro;
		set => SetField(ref _bairro, value ?? string.Empty);
	}

	public string Cidade
	{
		get => _cidade;
		set => SetField(ref _cidade, value ?? string.Empty);
	}

	public string Referencia
	{
		get => _referencia;
		set => SetField(ref _referencia, value ?? string.Empty);
	}
}
