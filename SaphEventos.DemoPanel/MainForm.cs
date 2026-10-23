using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SaphEventos;

namespace SaphEventos.DemoPanel;

public sealed class MainForm : Form
{
	const int ActionButtonWidth = 340;
	const int Panel1Min = 380;
	const int Panel2Min = 260;

	readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
	readonly TextBox _urlBox;
	readonly Label _statusLabel;
	readonly ChamadoEditorPanel _chamadoEditor;

	/// <summary>Novo chamado enviado no desvio de missão (aba «Desvio»).</summary>
	readonly ChamadoEditorPanel _desvioEditor;
	readonly SplitContainer _split;

	static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() },
	};

	public MainForm()
	{
		Text = "SAPH — Painel de eventos (demo)";
		Width = 980;
		Height = 780;
		MinimumSize = new Size(900, 680);
		StartPosition = FormStartPosition.CenterScreen;
		Font = new Font("Segoe UI", 9.5f);

		_split = new SplitContainer
		{
			Dock = DockStyle.Fill,
			Orientation = Orientation.Vertical,
			SplitterWidth = 6,
			Padding = new Padding(4),
		};
		Controls.Add(_split);
		Load += (_, _) => ApplySplitDistance();
		Shown += (_, _) => ApplySplitDistance();

		var leftGroup = new GroupBox
		{
			Text = "Dados do chamado",
			Dock = DockStyle.Fill,
			Padding = new Padding(10, 22, 10, 10),
		};
		_split.Panel1.Controls.Add(leftGroup);

		var tabs = new TabControl { Dock = DockStyle.Fill };
		leftGroup.Controls.Add(tabs);

		var tabAtual = new TabPage("Chamado atual") { Padding = new Padding(4, 8, 4, 4) };
		_chamadoEditor = new ChamadoEditorPanel();
		tabAtual.Controls.Add(_chamadoEditor);
		tabs.TabPages.Add(tabAtual);

		var tabDesvio = new TabPage("Desvio (novo chamado)") { Padding = new Padding(4, 8, 4, 4) };
		_desvioEditor = new ChamadoEditorPanel(DemoChamadoFactory.CreateDesvio());
		tabDesvio.Controls.Add(_desvioEditor);
		tabs.TabPages.Add(tabDesvio);

		var rightScroll = new Panel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
			Padding = new Padding(4, 0, 8, 8),
		};
		_split.Panel2.Controls.Add(rightScroll);

		var rightStack = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Dock = DockStyle.Top,
			Padding = new Padding(0),
		};
		rightScroll.Controls.Add(rightStack);

		rightStack.Controls.Add(new Label { Text = "URL da API", AutoSize = true, Margin = new Padding(0, 0, 0, 2) });
		_urlBox = new TextBox
		{
			Text = "http://localhost:5090",
			Width = ActionButtonWidth,
			Margin = new Padding(0, 0, 0, 8),
		};
		rightStack.Controls.Add(_urlBox);

		_statusLabel = new Label
		{
			Text = "Pronto.",
			AutoSize = false,
			Width = ActionButtonWidth,
			Height = 36,
			Margin = new Padding(0, 0, 0, 12),
		};
		rightStack.Controls.Add(_statusLabel);

		AddSection(rightStack, "Missão");
		AddMissionButton(rightStack, "Reset / Disponível", DemoRemoteCommandTipo.Reset);
		AddEventButton(rightStack, "1. Comunicado (usa formulário)", EEventoMissao.Comunicado, useFormPayload: true);
		AddEventButton(rightStack, "2. Ciente", EEventoMissao.Ciente);
		AddEventButton(rightStack, "3. Deslocando", EEventoMissao.Deslocando);
		AddEventButton(rightStack, "4. No Local", EEventoMissao.NoLocal);
		AddEventButton(rightStack, "5. Remover (Cliente)", EEventoMissao.RemoverPaciente);
		AddEventButton(rightStack, "6. Removendo", EEventoMissao.Removendo);
		AddEventButton(rightStack, "7. No Destino", EEventoMissao.NoDestino);

		AddSection(rightStack, "Desvio");
		var desvioBtn = CreateActionButton("Desvio de missão (usa aba Desvio)");
		desvioBtn.Click += async (_, _) => await SendAsync(new PostDemoEventRequest
		{
			Tipo = DemoRemoteCommandTipo.Desvio,
			Payload = _desvioEditor.BuildPayload(),
		});
		rightStack.Controls.Add(desvioBtn);

		AddSection(rightStack, "Protocolos");
		AddProtocolButton(rightStack, "Protocolo PCR", DemoRemoteProtocoloId.Pcr);
		AddProtocolButton(rightStack, "Protocolo Afogamento", DemoRemoteProtocoloId.Afogamento);
		AddProtocolButton(rightStack, "Protocolo Atropelamento", DemoRemoteProtocoloId.Atropelamento);
	}

	void ApplySplitDistance()
	{
		if (_split.Width <= 0)
			return;

		var max = _split.Width - Panel2Min - _split.SplitterWidth;
		if (max < Panel1Min)
			return;

		var target = (int)(_split.Width * 0.56);
		_split.SplitterDistance = Math.Clamp(target, Panel1Min, max);
		_split.Panel1MinSize = Panel1Min;
		_split.Panel2MinSize = Panel2Min;
	}

	static void AddSection(FlowLayoutPanel root, string title)
	{
		root.Controls.Add(new Label
		{
			Text = title,
			Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
			AutoSize = true,
			Margin = new Padding(0, 8, 0, 6),
		});
	}

	static Button CreateActionButton(string text)
	{
		return new Button
		{
			Text = text,
			Width = ActionButtonWidth,
			Height = 38,
			Margin = new Padding(0, 0, 0, 6),
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(10, 0, 0, 0),
		};
	}

	void AddMissionButton(FlowLayoutPanel root, string text, DemoRemoteCommandTipo tipo)
	{
		var btn = CreateActionButton(text);
		btn.Click += async (_, _) => await SendAsync(new PostDemoEventRequest { Tipo = tipo });
		root.Controls.Add(btn);
	}

	void AddEventButton(FlowLayoutPanel root, string text, EEventoMissao evento, bool useFormPayload = false)
	{
		var btn = CreateActionButton(text);
		btn.Click += async (_, _) => await SendAsync(new PostDemoEventRequest
		{
			Tipo = DemoRemoteCommandTipo.Missao,
			Evento = evento,
			Payload = useFormPayload ? _chamadoEditor.BuildPayload() : null,
		});
		root.Controls.Add(btn);
	}

	void AddProtocolButton(FlowLayoutPanel root, string text, DemoRemoteProtocoloId protocolo)
	{
		var btn = CreateActionButton(text);
		btn.Click += async (_, _) => await SendAsync(new PostDemoEventRequest
		{
			Tipo = DemoRemoteCommandTipo.Protocolo,
			Protocolo = protocolo,
		});
		root.Controls.Add(btn);
	}

	async Task SendAsync(PostDemoEventRequest body)
	{
		var baseUrl = _urlBox.Text.Trim().TrimEnd('/');
		if (baseUrl.Length == 0)
		{
			SetStatus("Informe a URL da API.");
			return;
		}

		try
		{
			using var response = await _http.PostAsJsonAsync($"{baseUrl}/api/demo/events", body, JsonOptions);
			var text = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				SetStatus($"HTTP {(int)response.StatusCode}: {text}");
				return;
			}

			var detalhe = body.Tipo == DemoRemoteCommandTipo.Desvio
				? body.Payload?.NumeroOcorrencia
				: body.Evento?.ToString() ?? body.Protocolo?.ToString();
			SetStatus($"OK — {body.Tipo} {detalhe ?? ""}");
		}
		catch (Exception ex)
		{
			SetStatus($"Erro: {ex.Message}");
		}
	}

	void SetStatus(string text) => _statusLabel.Text = text;
}
