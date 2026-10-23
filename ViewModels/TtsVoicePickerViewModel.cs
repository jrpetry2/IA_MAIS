using System.Collections.ObjectModel;
using System.Windows.Input;
using SaphMovel.Models;
using SaphMovel.Services.Voice;

namespace SaphMovel.ViewModels;

public sealed class TtsVoicePickerViewModel : ObservableModel
{
	readonly IChamadoSpeechService _speech;
	readonly Page _page;

	public TtsVoicePickerViewModel(IChamadoSpeechService speech, Page page)
	{
		_speech = speech;
		_page = page;
		CloseCommand = new Command(async () =>
		{
			_speech.Cancel();
			await _page.Navigation.PopModalAsync();
		});
		SelectAutomaticCommand = new Command(() =>
		{
			_speech.SelectVoice(null);
			SelectedKey = "";
			StatusText = "Voz: automática (PT-BR quando disponível)";
			RefreshSelectionFlags();
		});
		SelectVoiceCommand = new Command<TtsVoiceRow?>(row =>
		{
			if (row is null)
				return;
			_speech.SelectVoice(row.Option);
			SelectedKey = row.Key;
			StatusText = $"Voz: {row.Title}";
			RefreshSelectionFlags();
		});
		PreviewCommand = new Command<TtsVoiceRow?>(row =>
		{
			_ = _speech.SpeakPreviewAsync(
				"Olá equipe, este é um teste de voz do SAPH Móvel.",
				row?.Option);
		});
		RefreshCommand = new Command(async () => await LoadAsync());
	}

	public ObservableCollection<TtsVoiceRow> Voices { get; } = new();

	public ICommand CloseCommand { get; }
	public ICommand SelectAutomaticCommand { get; }
	public ICommand SelectVoiceCommand { get; }
	public ICommand PreviewCommand { get; }
	public ICommand RefreshCommand { get; }

	public string StatusText
	{
		get => _statusText;
		private set => SetField(ref _statusText, value);
	}

	string _statusText = "A carregar vozes…";

	public string SelectedKey
	{
		get => _selectedKey;
		private set => SetField(ref _selectedKey, value);
	}

	string _selectedKey = "";

	public bool IsBusy
	{
		get => _isBusy;
		private set => SetField(ref _isBusy, value);
	}

	bool _isBusy;

	public async Task LoadAsync()
	{
		IsBusy = true;
		StatusText = "A carregar vozes do sistema…";
		try
		{
			var list = await _speech.GetAvailableVoicesAsync().ConfigureAwait(true);
			var selected = _speech.GetSelectedVoiceOrNull(list);
			SelectedKey = selected?.Id ?? "";
			Voices.Clear();
			foreach (var v in list)
			{
				Voices.Add(new TtsVoiceRow
				{
					Option = v,
					Key = v.Id,
					Title = v.DisplayTitle,
					Subtitle = v.DisplaySubtitle,
					IsSelected = !string.IsNullOrEmpty(SelectedKey)
					             && string.Equals(v.Id, SelectedKey, StringComparison.OrdinalIgnoreCase),
				});
			}

			StatusText = selected is null
				? $"Automática · {list.Count} voz(es) no dispositivo"
				: $"Seleccionado: {selected.DisplayTitle} · {list.Count} voz(es)";
		}
		catch
		{
			StatusText = "Não foi possível listar as vozes TTS.";
		}
		finally
		{
			IsBusy = false;
		}
	}

	void RefreshSelectionFlags()
	{
		foreach (var row in Voices)
			row.IsSelected = !string.IsNullOrEmpty(SelectedKey)
			                 && string.Equals(row.Key, SelectedKey, StringComparison.OrdinalIgnoreCase);
	}

}

public sealed class TtsVoiceRow : ObservableModel
{
	public required TtsVoiceOption Option { get; init; }
	public required string Key { get; init; }
	public required string Title { get; init; }
	public required string Subtitle { get; init; }

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (!SetField(ref _isSelected, value))
				return;
			OnPropertyChanged(nameof(SelectionMark));
		}
	}

	bool _isSelected;

	public string SelectionMark => IsSelected ? "✓" : "";
}
