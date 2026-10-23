namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Avaliação Inicial (Boletim APH).</summary>
public sealed class AvaliacaoInicialFormData : ObservableModel
{
	string _reporteChegada = string.Empty;
	string _viasAereas = string.Empty;
	string _respiracao = string.Empty;
	string _circulacao = string.Empty;
	string _aberturaOcular = string.Empty;
	string _respostaMotora = string.Empty;
	string _respostaVerbal = string.Empty;

	public string ReporteChegada
	{
		get => _reporteChegada;
		set => SetField(ref _reporteChegada, value);
	}

	public string ViasAereas
	{
		get => _viasAereas;
		set => SetField(ref _viasAereas, value);
	}

	public string Respiracao
	{
		get => _respiracao;
		set => SetField(ref _respiracao, value);
	}

	public string Circulacao
	{
		get => _circulacao;
		set => SetField(ref _circulacao, value);
	}

	public string AberturaOcular
	{
		get => _aberturaOcular;
		set => SetField(ref _aberturaOcular, value);
	}

	public string RespostaMotora
	{
		get => _respostaMotora;
		set => SetField(ref _respostaMotora, value);
	}

	public string RespostaVerbal
	{
		get => _respostaVerbal;
		set => SetField(ref _respostaVerbal, value);
	}

	/// <summary>Limpa o formulário (novo atendimento / desvio de missão).</summary>
	public void Reset()
	{
		ReporteChegada = string.Empty;
		ViasAereas = string.Empty;
		Respiracao = string.Empty;
		Circulacao = string.Empty;
		AberturaOcular = string.Empty;
		RespostaMotora = string.Empty;
		RespostaVerbal = string.Empty;
	}

}
