using SaphEventos;

namespace SaphEventos.Api;

public sealed class DemoEventStore
{
	readonly object _gate = new();
	readonly List<DemoEventItem> _events = [];
	long _seq;

	public PostDemoEventResponse Enqueue(PostDemoEventRequest body)
	{
		var item = new DemoEventItem
		{
			Seq = Interlocked.Increment(ref _seq),
			CreatedAtUtc = DateTimeOffset.UtcNow,
			Tipo = body.Tipo,
			Evento = body.Evento,
			Payload = body.Payload,
			Protocolo = body.Protocolo,
		};

		lock (_gate)
		{
			_events.Add(item);
			while (_events.Count > 200)
				_events.RemoveAt(0);
		}

		return new PostDemoEventResponse { Seq = item.Seq, Ok = true };
	}

	public DemoEventsPollResponse Poll(long since)
	{
		lock (_gate)
		{
			var list = _events.Where(e => e.Seq > since).Take(50).ToList();
			var latest = _events.Count == 0 ? since : _events[^1].Seq;
			return new DemoEventsPollResponse
			{
				LatestSeq = latest,
				Events = list,
			};
		}
	}
}
