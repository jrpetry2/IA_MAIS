using System.Text.Json.Serialization;
using SaphStatus;
using SaphStatus.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
	o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
	o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<InMemoryStatusStore>();

var app = builder.Build();

app.MapPost("/api/status", (SubmitStatusRequest body, InMemoryStatusStore s) =>
{
	if (string.IsNullOrWhiteSpace(body.Imei))
		return Results.BadRequest("IMEI is required.");

	var entry = new StoredStatusEntry(body.Imei.Trim(), body.StatusType, DateTimeOffset.UtcNow);
	s.Add(entry);
	return Results.Ok(new { ok = true, receivedAt = entry.ReceivedAtUtc });
})
.WithName("SubmitStatus");

app.MapGet("/api/status", (InMemoryStatusStore s) => Results.Ok(s.Snapshot()))
.WithName("ListStatus");

app.MapGet("/", () => "SaphStatus.Api — POST /api/status { imei, statusType }");

app.Run();
