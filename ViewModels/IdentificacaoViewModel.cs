using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;
using SaphMovel.Services.Audio;
using SaphMovel.Services.Identificacao;
using SaphMovel.Services.OpenAi;

namespace SaphMovel.ViewModels;

public sealed class IdentificacaoViewModel : ObservableModel
{
	readonly MainViewModel _main;
	readonly Page _page;
	readonly IPressHoldAudioRecordService _recorder;
	readonly IOpenAiTranscriptionService _transcription;
	bool _isOuvirRecording;
	bool _isTranscribing;
	bool _sexoSugerido;
	int _transcriptionGeneration;
	int _ouvirPressGeneration;

	public bool IsOuvirRecording => _isOuvirRecording;

	public bool IsTranscribing => _isTranscribing;

	public string OuvirHintText => _isTranscribing
		? "A processar áudio e preencher a identificação…"
		: "A gravar — mantenha pressionado. Solte para terminar.";

	/// <summary>Sexo preenchido pela IA a partir do nome (não foi dito) — a equipa deve conferir.</summary>
	public bool ShowSexoSugeridoHint => _sexoSugerido;

	public IdentificacaoFormData Form => _main.Identificacao;

	public string PatientEmoji => _main.PatientEmoji;
	public string PatientSummary => _main.PatientSummary;

	public ObservableCollection<string> SexoItems { get; } = new(["", "Feminino", "Masculino"]);

	public ObservableCollection<string> CorItems { get; } =
		new(["", "Branca", "Preta", "Parda", "Amarela", "Indígena"]);

	public ICommand VoltarCommand { get; }
	public ICommand EnviarCommand { get; }

	public IdentificacaoViewModel(
		MainViewModel main,
		Page page,
		IPressHoldAudioRecordService recorder,
		IOpenAiTranscriptionService transcription)
	{
		_main = main;
		_page = page;
		_recorder = recorder;
		_transcription = transcription;
		VoltarCommand = new Command(async () => await VoltarAsync());
		EnviarCommand = new Command(async () => await EnviarAsync());
		_main.Identificacao.PropertyChanged += OnFormPropertyChanged;
		_main.PropertyChanged += OnMainPropertyChanged;
	}

	void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is null || e.PropertyName == nameof(MainViewModel.PatientSummary))
			OnPropertyChanged(nameof(PatientSummary));
	}

	public void Detach()
	{
		_main.Identificacao.PropertyChanged -= OnFormPropertyChanged;
		_main.PropertyChanged -= OnMainPropertyChanged;
	}

	public void RefreshSexoPicker() => OnPropertyChanged(nameof(SexoPickerItem));

	void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IdentificacaoFormData.Sexo))
			OnPropertyChanged(nameof(SexoPickerItem));
	}

	/// <summary>Texto do Picker de sexo (ligado a <see cref="IdentificacaoFormData.Sexo"/>).</summary>
	public string SexoPickerItem
	{
		get => Form.Sexo switch { "F" => "Feminino", "M" => "Masculino", _ => "" };
		set
		{
			var code = value == "Feminino" ? "F" : value == "Masculino" ? "M" : "";
			if (Form.Sexo == code)
				return;
			Form.Sexo = code;
			SetSexoSugerido(false);
			OnPropertyChanged(nameof(SexoPickerItem));
		}
	}

	public void OnOuvirPressed()
	{
		if (_isTranscribing)
			return;

		var gen = Interlocked.Increment(ref _ouvirPressGeneration);
		if (_isOuvirRecording || _recorder.IsRecording)
			return;

		SetOuvirRecording(true);
		_ = Task.Run(() => StartRecordingAsync(gen));
	}

	async Task StartRecordingAsync(int pressGeneration)
	{
		try
		{
			var ok = await _recorder.StartAsync().ConfigureAwait(false);
			if (pressGeneration != _ouvirPressGeneration)
				return;

			if (ok)
				return;

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (pressGeneration != _ouvirPressGeneration)
					return;
				SetOuvirRecording(false);
				await _page.DisplayAlertAsync(
					"Identificação",
					"Não foi possível gravar. Conceda permissão de microfone nas definições da app.",
					"OK");
			});
		}
		catch (Exception)
		{
			if (pressGeneration == _ouvirPressGeneration)
				await MainThread.InvokeOnMainThreadAsync(() => SetOuvirRecording(false));
		}
	}

	public void OnOuvirReleased()
	{
		Interlocked.Increment(ref _ouvirPressGeneration);

		if (!_isOuvirRecording && !_recorder.IsRecording)
			return;

		SetOuvirRecording(false);
		_ = Task.Run(StopRecordingAndTranscribeAsync);
	}

	async Task StopRecordingAndTranscribeAsync()
	{
		try
		{
			if (_recorder.IsRecording)
				await _recorder.StopAsync().ConfigureAwait(false);
		}
		catch (Exception)
		{
			/* ignore */
		}

		_ = TranscribeAndApplyIdentificacaoAsync();
	}

	async Task TranscribeAndApplyIdentificacaoAsync()
	{
		if (!File.Exists(_recorder.OutputFilePath))
			return;

		var generation = Interlocked.Increment(ref _transcriptionGeneration);
		SetTranscribing(true);

		try
		{
			var result = await _transcription
				.TranscribeIdentificacaoJsonAsync(_recorder.OutputFilePath)
				.ConfigureAwait(false);

			if (generation != _transcriptionGeneration)
				return;

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (generation != _transcriptionGeneration)
					return;

				if (!result.Success)
				{
					if (!string.IsNullOrWhiteSpace(result.Error))
						await _page.DisplayAlertAsync("Identificação", result.Error, "OK");
					return;
				}

				if (!IdentificacaoJsonMapper.TryApply(Form, result.IdentificacaoJson, out var sexoSugerido, out var err))
				{
					await _page.DisplayAlertAsync(
						"Identificação",
						err ?? "Não foi possível aplicar a identificação.",
						"OK");
					return;
				}

				SetSexoSugerido(sexoSugerido);
			});
		}
		finally
		{
			if (generation == _transcriptionGeneration)
				SetTranscribing(false);
		}
	}

	void SetSexoSugerido(bool sugerido)
	{
		if (_sexoSugerido == sugerido)
			return;
		_sexoSugerido = sugerido;
		OnPropertyChanged(nameof(ShowSexoSugeridoHint));
	}

	void SetTranscribing(bool transcribing)
	{
		if (_isTranscribing == transcribing)
			return;
		_isTranscribing = transcribing;
		OnPropertyChanged(nameof(IsTranscribing));
		OnPropertyChanged(nameof(OuvirHintText));
	}

	void SetOuvirRecording(bool recording)
	{
		if (_isOuvirRecording == recording)
			return;
		_isOuvirRecording = recording;
		OnPropertyChanged(nameof(IsOuvirRecording));
		OnPropertyChanged(nameof(OuvirHintText));
	}

	public void OnPageDisappearing()
	{
		Interlocked.Increment(ref _transcriptionGeneration);
		if (_recorder.IsRecording)
			_ = _recorder.StopAsync();
		SetOuvirRecording(false);
		SetTranscribing(false);
	}

	async Task VoltarAsync() => await _page.Navigation.PopModalAsync();

	async Task EnviarAsync()
	{
		var err = Validar();
		if (err is not null)
		{
			await _page.DisplayAlertAsync("Identificação", err, "OK");
			return;
		}

		_main.MarcarIdentificacaoBoletimConcluido();
		await _page.DisplayAlertAsync("Identificação", "Dados guardados em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}

	string? Validar()
	{
		if (string.IsNullOrWhiteSpace(Form.Nome))
			return "Preencha o nome do paciente.";
		if (Form.Sexo is not ("F" or "M"))
			return "Selecione o sexo.";
		if (string.IsNullOrWhiteSpace(Form.IdadeAnos) || !int.TryParse(Form.IdadeAnos.Trim(), out var anos) || anos < 0)
			return "Indique a idade em anos (número válido).";
		return null;
	}
}
