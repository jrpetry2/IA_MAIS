using System.Linq;
using Microsoft.Maui.Controls;

namespace SaphMovel;

/// <summary>Referência explícita à página atual para <c>DisplayAlert</c> (Shell nem sempre expõe o alvo em <c>Window.Page</c>).</summary>
public static class UiDialogHost
{
	static WeakReference<Page>? _registered;

	public static void Register(Page page) =>
		_registered = new WeakReference<Page>(page ?? throw new ArgumentNullException(nameof(page)));

	public static Page? GetPage()
	{
		if (_registered?.TryGetTarget(out var p) == true && p is not null)
			return p;
		if (Shell.Current?.CurrentPage is { } shellPage)
			return shellPage;
		return Application.Current?.Windows.FirstOrDefault()?.Page;
	}
}
