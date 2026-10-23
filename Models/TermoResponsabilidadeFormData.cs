namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Termo de Responsabilidade (sem alterar ✓ na lista do Boletim).</summary>
public sealed class TermoResponsabilidadeFormData : ObservableModel
{
	string _termoSelecionado = string.Empty;
	bool _assinaturasAdicionadas;

	public string TermoSelecionado
	{
		get => _termoSelecionado;
		set
		{
			if (!SetField(ref _termoSelecionado, value))
				return;
			OnPropertyChanged(nameof(TextoLegalCalculado));
		}
	}

	public bool AssinaturasAdicionadas
	{
		get => _assinaturasAdicionadas;
		set => SetField(ref _assinaturasAdicionadas, value);
	}

	public string TextoLegalCalculado => TermoResponsabilidadeTextos.Para(TermoSelecionado);

	/// <summary>Limpa o termo (novo atendimento / desvio de missão).</summary>
	public void Reset()
	{
		TermoSelecionado = string.Empty;
		AssinaturasAdicionadas = false;
	}
}
