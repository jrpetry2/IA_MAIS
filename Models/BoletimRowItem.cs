namespace SaphMovel.Models;

public sealed class BoletimRowItem : ObservableModel
{
	bool _isCompleted;

	public string Title { get; init; } = string.Empty;
	public bool IsActionRow { get; init; }
	public string StepKey { get; init; } = string.Empty;

	public bool IsCompleted
	{
		get => _isCompleted;
		set => SetField(ref _isCompleted, value);
	}
}
