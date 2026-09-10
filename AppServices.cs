using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SaphMovel.ViewModels;

namespace SaphMovel;

/// <summary>Resolves services for pages (Shell, etc.).</summary>
public static class AppServices
{
	static IServiceProvider? _services;

	public static void Initialize(IServiceProvider services) => _services = services;

	public static IServiceProvider Services =>
		_services ?? throw new InvalidOperationException("AppServices not initialized");

	[return: NotNull]
	public static T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

	public static MainViewModel CreateMainViewModel() =>
		_services?.GetService<MainViewModel>() ?? new MainViewModel();
}
