namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Impressão do Boletim (Boletim APH).</summary>
public sealed class ImpressaoBoletimFormData : ObservableModel
{
	bool _downloadArquivo = true;
	bool _copiaFisica = true;
	bool _enviarEmail = true;
	string _destinatario = string.Empty;
	string _email = string.Empty;
	string _pessoa = string.Empty;
	string _nome = string.Empty;
	string _tipoNumeroDocumento = string.Empty;
	string? _assinaturaStrokesJson;

	public bool DownloadArquivo
	{
		get => _downloadArquivo;
		set => SetField(ref _downloadArquivo, value);
	}

	public bool CopiaFisica
	{
		get => _copiaFisica;
		set => SetField(ref _copiaFisica, value);
	}

	public bool EnviarEmail
	{
		get => _enviarEmail;
		set => SetField(ref _enviarEmail, value);
	}

	public string Destinatario
	{
		get => _destinatario;
		set => SetField(ref _destinatario, value);
	}

	public string Email
	{
		get => _email;
		set => SetField(ref _email, value);
	}

	public string Pessoa
	{
		get => _pessoa;
		set => SetField(ref _pessoa, value);
	}

	public string Nome
	{
		get => _nome;
		set => SetField(ref _nome, value);
	}

	public string TipoNumeroDocumento
	{
		get => _tipoNumeroDocumento;
		set => SetField(ref _tipoNumeroDocumento, value);
	}

	/// <summary>Traços da assinatura (JSON) persistidos com Enviar; nulo = em branco.</summary>
	public string? AssinaturaStrokesJson
	{
		get => _assinaturaStrokesJson;
		set => SetField(ref _assinaturaStrokesJson, value);
	}

	/// <summary>Volta aos valores iniciais e apaga a assinatura (novo atendimento / desvio de missão).</summary>
	public void Reset()
	{
		DownloadArquivo = true;
		CopiaFisica = true;
		EnviarEmail = true;
		Destinatario = string.Empty;
		Email = string.Empty;
		Pessoa = string.Empty;
		Nome = string.Empty;
		TipoNumeroDocumento = string.Empty;
		AssinaturaStrokesJson = null;
	}

}
