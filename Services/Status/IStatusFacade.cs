using SaphStatus;

namespace SaphMovel.Services.Status;

/// <summary>Facade over remote status reporting (HTTP API). Callers use one method with IMEI + enum.</summary>
public interface IStatusFacade
{
	Task SubmitDeviceStatusAsync(string imei, EStatusType statusType, CancellationToken cancellationToken = default);
}
