using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SaphMovel.Models;

/// <summary>Base de <see cref="INotifyPropertyChanged"/> partilhada por ViewModels e formulários em memória.</summary>
public abstract class ObservableModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	/// <summary>Atribui e notifica se o valor mudou; devolve <c>true</c> quando houve alteração.</summary>
	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;
		field = value;
		OnPropertyChanged(name);
		return true;
	}
}
