using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SaphStatus;

namespace SaphMovel.Services.Status;

/// <summary>Calls <c>SaphStatus.Api</c> and keeps the HTTP details out of view models.</summary>
public sealed class StatusFacade : IStatusFacade
{
	static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() },
	};

	readonly HttpClient _http;

	public StatusFacade(HttpClient http) => _http = http;

	public async Task SubmitDeviceStatusAsync(string imei, EStatusType statusType, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(imei);

		using var response = await _http.PostAsJsonAsync(
			"api/status",
			new SubmitStatusRequest(imei.Trim(), statusType),
			SerializerOptions,
			cancellationToken).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();
	}
}
