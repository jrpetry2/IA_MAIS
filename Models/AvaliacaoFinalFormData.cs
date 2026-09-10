using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Avaliação Final (Boletim APH).</summary>
public sealed class AvaliacaoFinalFormData : INotifyPropertyChanged
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
