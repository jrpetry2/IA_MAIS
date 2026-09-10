using SaphStatus;

namespace SaphMovel.Services.Status;

/// <summary>No-op implementation when the API client is unavailable (e.g. design-time or offline).</summary>
public sealed class NullStatusFacade : IStatusFacade
{
	public static readonly NullStatusFacade Instance = new();

	NullStatusFacade() { }

	public Task SubmitDeviceStatusAsync(string imei, EStatusType statusType, CancellationToken cancellationToken = default) =>
		Task.CompletedTask;
}
