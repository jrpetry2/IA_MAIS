namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Avaliação Final (Boletim APH).</summary>
public sealed class AvaliacaoFinalFormData : ObservableModel
{
	string _textoAvaliacao = string.Empty;
	string _questaoRadio = string.Empty;
	bool _checkbox1;
	bool _checkbox2;
	bool _checkbox3;
	bool _checkbox4;

	public string TextoAvaliacao
	{
		get => _textoAvaliacao;
		set => SetField(ref _textoAvaliacao, value);
	}

	/// <summary>Texto do item selecionado no grupo tipo rádio (ex.: "Radio 2").</summary>
	public string QuestaoRadio
	{
		get => _questaoRadio;
		set => SetField(ref _questaoRadio, value);
	}

	public bool Checkbox1
	{
		get => _checkbox1;
		set => SetField(ref _checkbox1, value);
	}

	public bool Checkbox2
	{
		get => _checkbox2;
		set => SetField(ref _checkbox2, value);
	}

	public bool Checkbox3
	{
		get => _checkbox3;
		set => SetField(ref _checkbox3, value);
	}

	public bool Checkbox4
	{
		get => _checkbox4;
		set => SetField(ref _checkbox4, value);
	}

	/// <summary>Limpa o formulário (novo atendimento / desvio de missão).</summary>
	public void Reset()
	{
		TextoAvaliacao = string.Empty;
		QuestaoRadio = string.Empty;
		Checkbox1 = false;
		Checkbox2 = false;
		Checkbox3 = false;
		Checkbox4 = false;
	}

}
