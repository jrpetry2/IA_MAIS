using Microsoft.Maui.ApplicationModel;

namespace SaphMovel.Platforms.Android.Audio;

static class MainThreadHelper
{
	public static Task RunAsync(Action action)
	{
		if (MainThread.IsMainThread)
		{
			action();
			return Task.CompletedTask;
		}

		return MainThread.InvokeOnMainThreadAsync(action);
	}

	public static Task<T> RunAsync<T>(Func<T> func)
	{
		if (MainThread.IsMainThread)
			return Task.FromResult(func());

		return MainThread.InvokeOnMainThreadAsync(func);
	}
}
