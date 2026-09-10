using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;
using SaphMovel.Pages;

namespace SaphMovel.ViewModels;

public sealed class ImpressaoBoletimViewModel
{
	readonly MainViewModel _main;
	readonly ImpressaoBoletimPage _page;

	public ImpressaoBoletimFormData Form => _main.ImpressaoBoletim;

	public string PatientEmoji => _main.PatientEmoji;
	public string PatientSummary => _main.PatientSummary;

	public ObservableCollection<string> PessoaItems { get; } =
		new(["", "Paciente", "Responsável legal", "Acompanhante", "Outro"]);

	public ICommand VoltarCommand { get; }
	public ICommand EnviarCommand { get; }
	public ICommand LimparAssinaturaCommand { get; }

	public ImpressaoBoletimViewModel(MainViewModel main, ImpressaoBoletimPage page)
	{
		_main = main;
		_page = page;
		VoltarCommand = new Command(async () => await VoltarAsync());
		EnviarCommand = new Command(async () => await EnviarAsync());
		LimparAssinaturaCommand = new Command(() => _page.ClearSignaturePad());
	}

	async Task VoltarAsync() => await _page.Navigation.PopModalAsync();

	async Task EnviarAsync()
	{
		var err = Validar();
		if (err is not null)
		{
			await _page.DisplayAlertAsync("Impressão do Boletim", err, "OK");
			return;
		}

		// Guarda assinatura no estado partilhado (reabre a modal com a mesma assinatura).
		_page.SaveSignatureToForm();
		// Dados em memória (Form); não marcar IsCompleted na lista (sem ✓ verde nesta linha).
		await _page.DisplayAlertAsync("Impressão do Boletim", "Dados guardados em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}

	string? Validar()
	{
		if (!Form.DownloadArquivo && !Form.CopiaFisica && !Form.EnviarEmail)
			return "Selecione pelo menos uma opção de impressão ou envio.";
		if (string.IsNullOrWhiteSpace(Form.Destinatario))
			return "Preencha o destinatário.";
		if (string.IsNullOrWhiteSpace(Form.Email))
			return "Preencha o e-mail para envio.";
		if (string.IsNullOrWhiteSpace(Form.Pessoa))
			return "Selecione a pessoa na autorização.";
		if (string.IsNullOrWhiteSpace(Form.Nome))
			return "Preencha o nome na autorização.";
		if (string.IsNullOrWhiteSpace(Form.TipoNumeroDocumento))
			return "Preencha o tipo e número do documento.";
		if (!_page.SignatureHasInk)
			return "Assine no quadro antes de enviar.";
		return null;
	}
}
