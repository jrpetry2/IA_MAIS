using System.Text.Json;
using Microsoft.Maui.Graphics;

namespace SaphMovel;

/// <summary>Traços de assinatura desenhados num <see cref="Microsoft.Maui.Controls.GraphicsView"/>.</summary>
public sealed class SignatureDrawable : IDrawable
{
	static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

	readonly List<List<PointF>> _strokes = new();
	List<PointF>? _current;

	public bool HasInk =>
		_strokes.Any(s => s.Count >= 2) || (_current is { Count: >= 2 });

	/// <summary>Serializa traços (persistência do formulário).</summary>
	public string? ExportStrokesToJson()
	{
		if (!HasInk)
			return null;
		IEnumerable<List<PointF>> allStrokes = _strokes;
		if (_current is { Count: >= 2 })
			allStrokes = allStrokes.Append(_current);
		var payload = new AssinaturaStrokePayload
		{
			Strokes = allStrokes
				.Where(s => s.Count >= 2)
				.Select(s => s.Select(p => new AssinaturaPoint { X = p.X, Y = p.Y }).ToList())
				.ToList()
		};
		if (payload.Strokes.Count == 0)
			return null;
		return JsonSerializer.Serialize(payload, JsonOptions);
	}

	/// <summary>Restaura traços guardados; <paramref name="json"/> nula ou vazia limpa a área.</summary>
	public void LoadStrokesFromJson(string? json)
	{
		Clear();
		if (string.IsNullOrWhiteSpace(json))
			return;
		AssinaturaStrokePayload? payload;
		try
		{
			payload = JsonSerializer.Deserialize<AssinaturaStrokePayload>(json, JsonOptions);
		}
		catch
		{
			return;
		}
		if (payload?.Strokes is null)
			return;
		foreach (var stroke in payload.Strokes)
		{
			if (stroke is null || stroke.Count < 2)
				continue;
			_strokes.Add(stroke.Select(p => new PointF(p.X, p.Y)).ToList());
		}
	}

	public void BeginStroke(PointF p) => _current = new List<PointF> { p };

	public void AddPoint(PointF p) => _current?.Add(p);

	public void EndStroke()
	{
		if (_current is { Count: >= 2 })
			_strokes.Add(_current);
		_current = null;
	}

	public void Clear()
	{
		_strokes.Clear();
		_current = null;
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeLineCap = LineCap.Round;
		canvas.StrokeLineJoin = LineJoin.Round;
		canvas.StrokeColor = Colors.Black;
		canvas.StrokeSize = 3f;

		foreach (var stroke in _strokes)
			DrawStroke(canvas, stroke);
		if (_current is not null)
			DrawStroke(canvas, _current);
	}

	static void DrawStroke(ICanvas canvas, IReadOnlyList<PointF> pts)
	{
		if (pts.Count < 2)
			return;
		var path = new PathF(pts[0].X, pts[0].Y);
		for (var i = 1; i < pts.Count; i++)
			path.LineTo(pts[i].X, pts[i].Y);
		canvas.DrawPath(path);
	}
}

sealed class AssinaturaStrokePayload
{
	public List<List<AssinaturaPoint>>? Strokes { get; set; }
}

sealed class AssinaturaPoint
{
	public float X { get; set; }
	public float Y { get; set; }
}
