using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaphEventos;
using SaphMovel.Configuration;
using SaphMovel.Models;
using SaphMovel.Services.Missao;
using SaphMovel.ViewModels;

namespace SaphMovel.Services.Demo;

/// <summary>Poll da API de demo e aplica eventos na missão / protocolos.</summary>
public sealed class DemoEventRemoteSyncService : IDisposable
{
	readonly HttpClient _http;
	readonly IMissaoFacade _missao;
	readonly ILogger<DemoEventRemoteSyncService> _log;
	readonly EventosApiOptions _options;

	MainViewModel? _host;
	long _lastSeq;

	/// <summary>A API guarda o histórico em memória: o 1.º poll só sincroniza o seq, sem reaplicar eventos antigos.</summary>
	bool _seqSincronizado;

	CancellationTokenSource? _cts;
	Task? _loopTask;

	static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() },
	};

	public DemoEventRemoteSyncService(
		HttpClient http,
		IMissaoFacade missao,
		IOptions<EventosApiOptions> options,
		ILogger<DemoEventRemoteSyncService> log)
	{
		_http = http;
		_missao = missao;
		_log = log;
		_options = options.Value;
	}

	public void SetHost(MainViewModel? host) => _host = host;

	public void Start()
	{
		if (!_options.Enabled)
			return;
		Stop();
		_cts = new CancellationTokenSource();
		_loopTask = RunLoopAsync(_cts.Token);
	}

	public void Stop()
	{
		try
		{
			_cts?.Cancel();
		}
		catch
		{
			/* ignore */
		}

		_cts?.Dispose();
		_cts = null;
	}

	async Task RunLoopAsync(CancellationToken ct)
	{
		_log.LogInformation("Demo API: poll em {Url}", _options.BaseUrl);
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await PollOnceAsync(ct).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_log.LogDebug(ex, "Demo API: falha no poll.");
			}

			try
			{
				await Task.Delay(Math.Max(300, _options.PollIntervalMs), ct).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	async Task PollOnceAsync(CancellationToken ct)
	{
		var url = $"api/demo/events?since={_lastSeq}";
		using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
			return;

		var body = await response.Content.ReadFromJsonAsync<DemoEventsPollResponse>(JsonOptions, ct)
			.ConfigureAwait(false);
		if (body is null)
			return;

		// Eventos enviados antes de o app abrir (ex.: desvio de um teste anterior) não são aplicados.
		if (!_seqSincronizado)
		{
			_seqSincronizado = true;
			_lastSeq = Math.Max(_lastSeq, body.LatestSeq);
			_log.LogInformation("Demo API: sincronizado em seq={Seq}; eventos anteriores ignorados.", _lastSeq);
			return;
		}

		if (body.Events is null || body.Events.Count == 0)
			return;

		foreach (var item in body.Events.OrderBy(e => e.Seq))
		{
			if (item.Seq <= _lastSeq)
				continue;
			_lastSeq = item.Seq;
			ApplyOnMainThread(item);
		}

		if (body.LatestSeq > _lastSeq)
			_lastSeq = body.LatestSeq;
	}

	void ApplyOnMainThread(DemoEventItem item)
	{
		if (MainThread.IsMainThread)
			Apply(item);
		else
			MainThread.BeginInvokeOnMainThread(() => Apply(item));
	}

	void Apply(DemoEventItem item)
	{
		_log.LogInformation("Demo API: aplicar seq={Seq} tipo={Tipo}", item.Seq, item.Tipo);
		switch (item.Tipo)
		{
			case DemoRemoteCommandTipo.Reset:
				_missao.Reset();
				return;
			case DemoRemoteCommandTipo.Missao when item.Evento is not null:
				_missao.Publish(item.Evento.Value, item.Payload);
				return;
			case DemoRemoteCommandTipo.Desvio when item.Payload is not null:
				_missao.Desviar(item.Payload);
				return;
			case DemoRemoteCommandTipo.Protocolo when item.Protocolo is not null:
				if (_host is null)
				{
					_log.LogWarning("Demo API: protocolo sem MainViewModel ligado.");
					return;
				}

				var id = item.Protocolo.Value switch
				{
					DemoRemoteProtocoloId.Pcr => ProtocoloAtendimentoId.Pcr,
					DemoRemoteProtocoloId.Afogamento => ProtocoloAtendimentoId.Afogamento,
					DemoRemoteProtocoloId.Atropelamento => ProtocoloAtendimentoId.Atropelamento,
					_ => ProtocoloAtendimentoId.Pcr,
				};
				_host.AtivarProtocolo(id);
				return;
		}
	}

	public void Dispose() => Stop();
}
