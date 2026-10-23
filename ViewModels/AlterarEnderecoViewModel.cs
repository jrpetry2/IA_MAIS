using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;
using SaphMovel.Services.Audio;
using SaphMovel.Services.Endereco;
using SaphMovel.Services.OpenAi;

namespace SaphMovel.ViewModels;

public sealed class AlterarEnderecoViewModel : ObservableModel
{
	readonly MainViewModel _main;
	readonly Page _page;
	readonly IPressHoldAudioRecordService _recorder;
	readonly IOpenAiTranscriptionService _transcription;
	readonly EnderecoFormData _form = new();
	bool _isOuvirRecording;
	bool _isTranscribing;
	int _transcriptionGeneration;
	int _ouvirPressGeneration;

	public EnderecoFormData Form => _form;

	public bool IsOuvirRecording => _isOuvirRecording;

	public string OuvirHintText => _isTranscribing
		? "A processar áudio e preencher o endereço…"
		: "A gravar — mantenha pressionado. Solte para terminar.";

	public bool IsTranscribing => _isTranscribing;

	public ICommand VoltarCommand { get; }
	public ICommand AtualizarCommand { get; }

	public AlterarEnderecoViewModel(
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
		AtualizarCommand = new Command(async () => await AtualizarAsync());
		LoadFromMain();
	}

	void LoadFromMain()
	{
		_form.Logradouro = _main.ChamadoLogradouro;
		_form.Numero = _main.ChamadoNumero;
		_form.Bairro = _main.ChamadoBairro;
		_form.Cidade = _main.ChamadoCidade;
		_form.Referencia = DisplayOrEmpty(_main.ChamadoReferencia);
	}

	static string DisplayOrEmpty(string value) =>
		string.IsNullOrWhiteSpace(value) || value == "—" ? "" : value.Trim();

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
					"Alterar Endereço",
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

		_ = TranscribeAndApplyAddressAsync();
	}

	async Task TranscribeAndApplyAddressAsync()
	{
		if (!File.Exists(_recorder.OutputFilePath))
			return;

		var generation = Interlocked.Increment(ref _transcriptionGeneration);
		SetTranscribing(true);

		try
		{
			var result = await _transcription
				.TranscribeAddressJsonAsync(_recorder.OutputFilePath)
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
						await _page.DisplayAlertAsync("Alterar Endereço", result.Error, "OK");
					return;
				}

				if (!EnderecoJsonMapper.TryApply(_form, result.AddressJson, out var err))
				{
					await _page.DisplayAlertAsync(
						"Alterar Endereço",
						err ?? "Não foi possível aplicar o endereço.",
						"OK");
				}
			});
		}
		finally
		{
			if (generation == _transcriptionGeneration)
				SetTranscribing(false);
		}
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

	async Task AtualizarAsync()
	{
		if (string.IsNullOrWhiteSpace(_form.Logradouro) && string.IsNullOrWhiteSpace(_form.Cidade))
		{
			await _page.DisplayAlertAsync(
				"Alterar Endereço",
				"Preencha pelo menos o logradouro ou a cidade.",
				"OK");
			return;
		}

		_main.ApplyEnderecoUpdate(
			_form.Logradouro,
			_form.Numero,
			_form.Bairro,
			_form.Cidade,
			_form.Referencia);

		await _page.DisplayAlertAsync("Alterar Endereço", "Endereço actualizado em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}
}
