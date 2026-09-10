using SaphStatus;

namespace SaphStatus.Api;

public sealed class InMemoryStatusStore
{
	readonly object _gate = new();
	readonly List<StoredStatusEntry> _entries = new();

	public void Add(StoredStatusEntry entry)
	{
		lock (_gate)
			_entries.Add(entry);
	}

	public IReadOnlyList<StoredStatusEntry> Snapshot()
	{
		lock (_gate)
			return _entries.ToList();
	}
}

public sealed record StoredStatusEntry(string Imei, EStatusType StatusType, DateTimeOffset ReceivedAtUtc);
