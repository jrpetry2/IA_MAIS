using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

// Uso:
//   (sem args)  → Resources\Images\*.png (a partir de bin/…/net10.0\)
//   <pasta>     → essa pasta\*.png
//   <ficheiro>  → só esse .png
//   --flood <ficheiro>  → BFS a partir da borda (ilustrações com fundo em caixa, stock)
string? targetPath = null;
var useFlood = false;
if (args.Length == 0)
{ /* default dir */ }
else
{
	if (string.Equals(args[0], "--flood", StringComparison.OrdinalIgnoreCase))
	{
		useFlood = true;
		if (args.Length < 2)
		{
			Console.Error.WriteLine("Uso: --flood <ficheiro.png>");
			return 1;
		}
		targetPath = Path.GetFullPath(args[1]);
	}
	else
	{
		if (string.Equals(args[0], "-f", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
		{
			useFlood = true;
			targetPath = Path.GetFullPath(args[1]);
		}
		else
			targetPath = Path.GetFullPath(args[0]);
	}
}

if (targetPath is not null
    && string.Equals(Path.GetFileName(targetPath), "status_aguardando_regulacao.png", StringComparison.OrdinalIgnoreCase)
    && !useFlood)
	useFlood = true;

var defaultImagesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Images"));

IEnumerable<string> files;
if (targetPath is not null)
{
	if (File.Exists(targetPath) && targetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
		files = [targetPath];
	else if (Directory.Exists(targetPath))
		files = Directory.EnumerateFiles(targetPath, "*.png");
	else
	{
		Console.Error.WriteLine($"Caminho não encontrado: {targetPath}");
		return 1;
	}
}
else
{
	if (!Directory.Exists(defaultImagesDir))
	{
		Console.Error.WriteLine($"Pasta por defeito não encontrada: {defaultImagesDir}");
		return 1;
	}
	files = Directory.EnumerateFiles(defaultImagesDir, "*.png");
}

foreach (var path in files)
{
	var fileFlood = useFlood
		|| string.Equals(Path.GetFileName(path), "status_aguardando_regulacao.png", StringComparison.OrdinalIgnoreCase);

	if (fileFlood)
	{
		RemoveBackgroundFloodFromEdges(path);
		Console.WriteLine($"OK (flood): {Path.GetFileName(path)}");
	}
	else
	{
		ApplyLuminanceChromaToPlainBackground(path);
		Console.WriteLine($"OK: {Path.GetFileName(path)}");
	}
}

return 0;

// --- Luminância: ícones 48x48, brancos puros; não resolve bem ilustrações com #F0F0F0 ou bordas. ---
static void ApplyLuminanceChromaToPlainBackground(string path)
{
	// Foi relaxado ligeiramente; ícones pequenos toleram o intervalo.
	const float minLuminance = 0.80f;
	const float maxChroma = 32f;
	using var image = Image.Load<Rgba32>(path);
	image.ProcessPixelRows(accessor =>
	{
		for (int y = 0; y < accessor.Height; y++)
		{
			var row = accessor.GetRowSpan(y);
			for (int x = 0; x < row.Length; x++)
			{
				ref var p = ref row[x];
				var minc = Min3(p.R, p.G, p.B);
				var maxb = Max3(p.R, p.G, p.B);
				float chroma = maxb - minc;
				float lum = (0.299f * p.R + 0.587f * p.G + 0.114f * p.B) / 255f;
				if (lum >= minLuminance && chroma <= maxChroma)
				{
					float howBg = Math.Clamp((lum - minLuminance) / (1f - minLuminance), 0f, 1f);
					p.A = (byte)Math.Round(p.A * (1f - howBg));
				}
			}
		}
	});
	image.SaveAsPngAsync(path, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression })
		.GetAwaiter()
		.GetResult();
}

// --- BFS a partir de todos os píxeis de borda: remove só o fundo ligado ao exterior (área "branco caixa"). ---
// chroma e lum limitam para não invadir pele (R>G>B) ou azul da roupa.
static void RemoveBackgroundFloodFromEdges(string path)
{
	using var image = Image.Load<Rgba32>(path);
	int w = image.Width;
	int h = image.Height;
	if (w < 2 || h < 2)
	{
		ApplyLuminanceChromaToPlainBackground(path);
		return;
	}

	// 1) marcar fundo
	var isBg = new bool[w, h];
	for (int y = 0; y < h; y++)
	{
		for (int x = 0; x < w; x++)
		{
			var p = image[x, y];
			isBg[x, y] = IsBackgroundFlood((float)p.R, (float)p.G, (float)p.B, (float)p.A);
		}
	}

	var inQueue = new bool[w, h];
	var q = new Queue<(int x, int y)>();
	void TryEnqueue(int x, int y)
	{
		if (x < 0 || x >= w || y < 0 || y >= h)
			return;
		if (inQueue[x, y] || !isBg[x, y])
			return;
		inQueue[x, y] = true;
		q.Enqueue((x, y));
	}

	for (int x = 0; x < w; x++)
	{
		TryEnqueue(x, 0);
		TryEnqueue(x, h - 1);
	}
	for (int y = 0; y < h; y++)
	{
		TryEnqueue(0, y);
		TryEnqueue(w - 1, y);
	}

	while (q.Count > 0)
	{
		var (x, y) = q.Dequeue();
		if (!isBg[x, y])
			continue;
		TryEnqueue(x - 1, y);
		TryEnqueue(x + 1, y);
		TryEnqueue(x, y - 1);
		TryEnqueue(x, y + 1);
	}

	// 2) apagar: em região flood, alpha = 0. Suavizar 1px na fronteira melhora a borda, mas
	//    aqui: só marcamos píxeis inQueue (BFS) que passam isBg; fronteira fica nítida.
	for (int y = 0; y < h; y++)
	{
		for (int x = 0; x < w; x++)
		{
			if (inQueue[x, y] && isBg[x, y])
			{
				var p = image[x, y];
				p.A = 0;
				image[x, y] = p;
			}
		}
	}

	// 3) Premultiplied → straight para guardar ; ImageSharp Rgba32 é directo, alpha 0 é suficiente
	image.SaveAsPngAsync(path, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression })
		.GetAwaiter()
		.GetResult();
}

/// <summary>
/// "Fundo" de storyboard: NEUTROS muito claros, baixa croma. Exclui pele (~croma>24) e tudo colorido.
/// </summary>
static bool IsBackgroundFlood(float r, float g, float b, float a)
{
	_ = a;
	var maxB = MathF.Max(r, MathF.Max(g, b));
	var minB = MathF.Min(r, MathF.Min(g, b));
	var chroma = maxB - minB;
	var lum = (0.299f * r + 0.587f * g + 0.114f * b) / 255f;
	// Sombra ainda escura: não rastejar
	if (lum < 0.62f)
		return false;
	// Tudo o que tiver croma "real" fica: roupa, traços, ilustração
	if (chroma > 24f)
		return false;
	// Tons muito altos, quase gris neutro (paredes, tabuleiros claros) ligados à borda
	if (chroma <= 20f && lum >= 0.64f)
		return true;
	// Ainda claro, chroma 21–24: só se quase branco
	if (lum >= 0.90f)
		return true;
	return false;
}

static int Min3(byte a, byte b, byte c) => a <= b && a <= c ? a : (b <= c ? b : c);
static int Max3(byte a, byte b, byte c) => a >= b && a >= c ? a : (b >= c ? b : c);
