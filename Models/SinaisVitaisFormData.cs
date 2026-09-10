using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SaphMovel.Models;

/// <summary>Estado em memória do formulário Sinais Vitais (Boletim APH).</summary>
public sealed class SinaisVitaisFormData : INotifyPropertyChanged
{
	string _pas = string.Empty;
	string _pad = string.Empty;
	string _fc = string.Empty;
	string _fr = string.Empty;
	string _satO2 = string.Empty;
	string _tempAx = string.Empty;
	string _hgt = string.Empty;
	string _glasgow = string.Empty;
	string _rts = string.Empty;
	string _pts = string.Empty;
	string _evolucao = string.Empty;

	public string Pas
	{
		get => _pas;
		set => SetField(ref _pas, value);
	}

	public string Pad
	{
		get => _pad;
		set => SetField(ref _pad, value);
	}

	public string Fc
	{
		get => _fc;
		set => SetField(ref _fc, value);
	}

	public string Fr
	{
		get => _fr;
		set => SetField(ref _fr, value);
	}

	public string SatO2
	{
		get => _satO2;
		set => SetField(ref _satO2, value);
	}

	public string TempAx
	{
		get => _tempAx;
		set => SetField(ref _tempAx, value);
	}

	public string Hgt
	{
		get => _hgt;
		set => SetField(ref _hgt, value);
	}

	public string Glasgow
	{
		get => _glasgow;
		set => SetField(ref _glasgow, value);
	}

	public string Rts
	{
		get => _rts;
		set => SetField(ref _rts, value);
	}

	public string Pts
	{
		get => _pts;
		set => SetField(ref _pts, value);
	}

	public string Evolucao
	{
		get => _evolucao;
		set => SetField(ref _evolucao, value);
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
