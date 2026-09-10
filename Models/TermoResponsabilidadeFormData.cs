using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Termo de Responsabilidade (sem alterar ✓ na lista do Boletim).</summary>
public sealed class TermoResponsabilidadeFormData : INotifyPropertyChanged
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

	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged([CallerMemberName] string? name = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;
		field = value;
		OnPropertyChanged(name);
		return true;
	}
}
