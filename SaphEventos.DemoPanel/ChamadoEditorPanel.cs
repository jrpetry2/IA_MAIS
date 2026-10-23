using SaphEventos;

namespace SaphEventos.DemoPanel;

/// <summary>Formulário editável dos dados do chamado (demo).</summary>
public sealed class ChamadoEditorPanel : UserControl
{
	const int LabelColumnWidth = 148;
	const int FieldMinWidth = 280;

	readonly TextBox _numeroOcorrencia;
	readonly TextBox _nomeEquipe;
	readonly TextBox _logradouro;
	readonly TextBox _numero;
	readonly TextBox _bairro;
	readonly TextBox _cidade;
	readonly TextBox _referencia;
	readonly TextBox _nomePaciente;
	readonly TextBox _idade;
	readonly ComboBox _sexo;
	readonly TextBox _tipoSocorro;
	readonly TextBox _acionamento;
	readonly TextBox _orientacao;
	readonly TextBox _apoioPor;
	readonly TableLayoutPanel _grid;

	public ChamadoEditorPanel()
		: this(DemoChamadoFactory.Create())
	{
	}

	/// <param name="initial">Chamado carregado ao abrir (ex.: modelo de desvio na aba Desvio).</param>
	public ChamadoEditorPanel(ChamadoPayload initial)
	{
		Dock = DockStyle.Fill;
		Padding = new Padding(4, 0, 8, 8);

		var presets = new FlowLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = true,
			Padding = new Padding(0, 0, 0, 10),
		};
		Controls.Add(presets);
		AddPresetButton(presets, "Modelo padrão", () => LoadFrom(DemoChamadoFactory.Create()));
		AddPresetButton(presets, "Modelo alternativo", () => LoadFrom(DemoChamadoFactory.CreateAlternativo()));
		AddPresetButton(presets, "Modelo desvio", () => LoadFrom(DemoChamadoFactory.CreateDesvio()));

		var scrollHost = new Panel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
		};
		Controls.Add(scrollHost);

		_grid = new TableLayoutPanel
		{
			ColumnCount = 2,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Dock = DockStyle.Top,
			Padding = new Padding(0),
		};
		_grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumnWidth));
		_grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		scrollHost.Controls.Add(_grid);

		void SyncGridWidth()
		{
			var w = Math.Max(FieldMinWidth + LabelColumnWidth + 24, scrollHost.ClientSize.Width - 4);
			_grid.Width = w;
			_grid.MinimumSize = new Size(w, 0);
		}

		scrollHost.Resize += (_, _) => SyncGridWidth();
		Resize += (_, _) => SyncGridWidth();

		int row = 0;
		_numeroOcorrencia = AddRow(ref row, "Nº ocorrência");
		_nomeEquipe = AddRow(ref row, "Equipe");
		_logradouro = AddRow(ref row, "Logradouro");
		_numero = AddRow(ref row, "Número");
		_bairro = AddRow(ref row, "Bairro");
		_cidade = AddRow(ref row, "Cidade");
		_referencia = AddRow(ref row, "Referência");
		_nomePaciente = AddRow(ref row, "Paciente");
		_idade = AddRow(ref row, "Idade");
		_sexo = AddComboRow(ref row, "Sexo", ["", "M", "F"]);
		_tipoSocorro = AddRow(ref row, "Tipo de socorro");
		_acionamento = AddRow(ref row, "Acionamento");
		_apoioPor = AddRow(ref row, "Apoio por");
		_orientacao = AddMultilineRow(ref row, "Orientação regulação");

		LoadFrom(initial);
		SyncGridWidth();
	}

	TextBox AddRow(ref int row, string label)
	{
		AddLabelRow(row, label);
		var box = new TextBox
		{
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
			Width = FieldMinWidth,
			Margin = new Padding(0, 3, 0, 7),
		};
		_grid.Controls.Add(box, 1, row);
		_grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;
		return box;
	}

	ComboBox AddComboRow(ref int row, string label, string[] items)
	{
		AddLabelRow(row, label);
		var combo = new ComboBox
		{
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
			Width = FieldMinWidth,
			DropDownStyle = ComboBoxStyle.DropDownList,
			Margin = new Padding(0, 3, 0, 7),
		};
		combo.Items.AddRange(items);
		combo.SelectedIndex = 0;
		_grid.Controls.Add(combo, 1, row);
		_grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;
		return combo;
	}

	TextBox AddMultilineRow(ref int row, string label)
	{
		AddLabelRow(row, label);
		var box = new TextBox
		{
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
			Width = FieldMinWidth,
			Height = 80,
			Multiline = true,
			ScrollBars = ScrollBars.Vertical,
			Margin = new Padding(0, 3, 0, 7),
		};
		_grid.Controls.Add(box, 1, row);
		_grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;
		return box;
	}

	void AddLabelRow(int row, string text)
	{
		_grid.Controls.Add(new Label
		{
			Text = text,
			AutoSize = false,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(0, 6, 8, 0),
		}, 0, row);
	}

	static void AddPresetButton(FlowLayoutPanel panel, string text, Action onClick)
	{
		var btn = new Button
		{
			Text = text,
			AutoSize = true,
			MinimumSize = new Size(120, 34),
			Margin = new Padding(0, 0, 8, 0),
		};
		btn.Click += (_, _) => onClick();
		panel.Controls.Add(btn);
	}

	public void LoadFrom(ChamadoPayload source)
	{
		source.EnsureDisplayFields();
		_numeroOcorrencia.Text = source.NumeroOcorrencia;
		_nomeEquipe.Text = source.NomeEquipe;
		_logradouro.Text = source.Logradouro;
		_numero.Text = source.Numero;
		_bairro.Text = source.Bairro;
		_cidade.Text = source.Cidade;
		_referencia.Text = source.Referencia;
		_nomePaciente.Text = source.NomePaciente;
		_idade.Text = source.Idade;
		var s = string.IsNullOrWhiteSpace(source.Sexo) ? "" : source.Sexo.Trim().ToUpperInvariant();
		if (s.Length > 0)
			s = s[..1];
		_sexo.SelectedItem = s is "M" or "F" ? s : "";
		_tipoSocorro.Text = string.IsNullOrWhiteSpace(source.TipoDeSocorro)
			? source.Atendimento
			: source.TipoDeSocorro;
		_acionamento.Text = source.Acionamento;
		_orientacao.Text = source.OrientacaoRegulacao;
		_apoioPor.Text = source.ApoioPor;
	}

	public ChamadoPayload BuildPayload()
	{
		var sexo = _sexo.SelectedItem?.ToString() ?? "";
		var p = new ChamadoPayload
		{
			NumeroOcorrencia = _numeroOcorrencia.Text.Trim(),
			NomeEquipe = _nomeEquipe.Text.Trim(),
			Logradouro = _logradouro.Text.Trim(),
			Numero = _numero.Text.Trim(),
			Bairro = _bairro.Text.Trim(),
			Cidade = _cidade.Text.Trim(),
			Referencia = _referencia.Text.Trim(),
			NomePaciente = _nomePaciente.Text.Trim(),
			Idade = _idade.Text.Trim(),
			Sexo = sexo,
			TipoDeSocorro = _tipoSocorro.Text.Trim(),
			Atendimento = _tipoSocorro.Text.Trim(),
			Acionamento = _acionamento.Text.Trim(),
			OrientacaoRegulacao = _orientacao.Text.Trim(),
			ApoioPor = _apoioPor.Text.Trim(),
			DescricaoEvento = "Deslocar",
			DataHoraEvento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
			TipoCliente = "Demo",
			CodigoCliente = "0",
		};
		p.EnsureDisplayFields();
		return p;
	}
}
