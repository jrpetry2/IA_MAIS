using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SaphMovel.Models;

public sealed class BoletimRowItem : INotifyPropertyChanged
{
	bool _isCompleted;

	public string Title { get; init; } = string.Empty;
	public bool IsActionRow { get; init; }
	public string StepKey { get; init; } = string.Empty;

	public bool IsCompleted
	{
		get => _isCompleted;
		set
		{
			if (_isCompleted == value)
				return;
			_isCompleted = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged([CallerMemberName] string? name = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
