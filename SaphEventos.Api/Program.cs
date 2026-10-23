using System.Text.Json.Serialization;
using SaphEventos;
using SaphEventos.Api;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5090");

builder.Services.ConfigureHttpJsonOptions(o =>
{
	o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
	o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<DemoEventStore>();

var app = builder.Build();

app.MapPost("/api/demo/events", (PostDemoEventRequest body, DemoEventStore store) =>
{
	if (body.Tipo == DemoRemoteCommandTipo.Missao && body.Evento is null)
		return Results.BadRequest("evento é obrigatório para tipo missao.");
	if (body.Tipo == DemoRemoteCommandTipo.Protocolo && body.Protocolo is null)
		return Results.BadRequest("protocolo é obrigatório para tipo protocolo.");
	if (body.Tipo == DemoRemoteCommandTipo.Desvio && body.Payload is null)
		return Results.BadRequest("payload é obrigatório para tipo desvio.");

	var result = store.Enqueue(body);
	return Results.Ok(result);
});

app.MapGet("/api/demo/events", (long since, DemoEventStore store) =>
	Results.Ok(store.Poll(since)));

app.MapGet("/", () =>
	"SaphEventos.Api — POST /api/demo/events · GET /api/demo/events?since=0");

app.Run();
