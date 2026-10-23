using System.Collections.ObjectModel;
using System.Windows.Input;
using SaphEventos;
using SaphMovel.Models;
using SaphMovel.Services.Missao;

namespace SaphMovel.ViewModels;

public sealed class DevEventSimulatorViewModel : ObservableModel
{
	readonly IMissaoFacade _missao;
	readonly MainViewModel _main;
	readonly Page _page;

	public DevEventSimulatorViewModel(IMissaoFacade missao, MainViewModel main, Page page)
	{
		_missao = missao;
		_main = main;
		_page = page;
		PublishCommand = new Command<string>(OnPublish, CanPublish);
		ResetCommand = new Command(() =>
		{
			_missao.Reset();
			Refresh();
		});
		DesvioCommand = new Command(() =>
		{
			// O desvio fecha os modais abertos (incluindo este simulador) — o utilizador vê logo o novo chamado.
			_missao.Desviar(DemoChamadoFactory.CreateDesvio());
		});
		CloseCommand = new Command(async () => await _page.Navigation.PopModalAsync());
		ReceiveTransporteCommand = new Command(() =>
		{
			var pacote = DemoPrescricaoFactory.Transporte();
			_main.ApplyPrescricaoPacote(pacote);
			PrescricaoStatus = $"Recebido: {pacote.Nome}";
		});
		ReceiveProtocoloPcrCommand = new Command(() =>
		{
			_main.AtivarProtocoloPcr();
			PrescricaoStatus = "Recebido: Protocolo PCR";
		});
		ReceiveProtocoloAfogamentoCommand = new Command(() =>
		{
			_main.AtivarProtocoloAfogamento();
			PrescricaoStatus = "Recebido: Protocolo Afogamento";
		});
		ReceiveProtocoloAtropelamentoCommand = new Command(() =>
		{
			_main.AtivarProtocoloAtropelamento();
			PrescricaoStatus = "Recebido: Protocolo Atropelamento";
		});
		ReceiveMedicamentosCommand = new Command(() =>
		{
			var pacote = DemoPrescricaoFactory.Medicamentos();
			_main.ApplyPrescricaoPacote(pacote);
			PrescricaoStatus = $"Recebido: {pacote.Nome}";
		});
		ClearPrescricoesCommand = new Command(() =>
		{
			_main.ClearPrescricoes();
			PrescricaoStatus = "Prescrições limpas";
		});
		_missao.Changed += OnMissaoChanged;
		Refresh();
	}

	public ICommand PublishCommand { get; }
	public ICommand ResetCommand { get; }
	public ICommand DesvioCommand { get; }
	public ICommand CloseCommand { get; }
	public ICommand ReceiveTransporteCommand { get; }
	public ICommand ReceiveProtocoloPcrCommand { get; }
	public ICommand ReceiveProtocoloAfogamentoCommand { get; }
	public ICommand ReceiveProtocoloAtropelamentoCommand { get; }
	public ICommand ReceiveMedicamentosCommand { get; }
	public ICommand ClearPrescricoesCommand { get; }

	public string EstadoAtual
	{
		get => _estadoAtual;
		private set => SetField(ref _estadoAtual, value);
	}

	string _estadoAtual = "";

	public string PrescricaoStatus
	{
		get => _prescricaoStatus;
		private set => SetField(ref _prescricaoStatus, value);
	}

	string _prescricaoStatus = "Nenhum recebimento nesta sessão.";

	public ObservableCollection<SimEventButton> Botoes { get; } = new();

	public void Detach() => _missao.Changed -= OnMissaoChanged;

	void OnMissaoChanged(MissaoSnapshot _) =>
		MainThread.BeginInvokeOnMainThread(Refresh);

	void Refresh()
	{
		var snap = _missao.Atual;
		EstadoAtual = $"{snap.Evento} → {MissaoFacade.ToStatusScene(snap.Evento)}";
		var next = _missao.ProximosEventos();
		Botoes.Clear();
		foreach (var e in AllButtons())
		{
			Botoes.Add(new SimEventButton
			{
				Evento = e,
				Titulo = Titulo(e),
				IsEnabled = next.Contains(e) || (e == EEventoMissao.Disponivel && snap.Evento != EEventoMissao.Disponivel),
			});
		}

		((Command)PublishCommand).ChangeCanExecute();
	}

	static IEnumerable<EEventoMissao> AllButtons() =>
	[
		EEventoMissao.Comunicado,
		EEventoMissao.Ciente,
		EEventoMissao.Deslocando,
		EEventoMissao.NoLocal,
		EEventoMissao.RemoverPaciente,
		EEventoMissao.Removendo,
		EEventoMissao.NoDestino,
	];

	static string Titulo(EEventoMissao e) => e switch
	{
		EEventoMissao.Comunicado => "1. Comunicado",
		EEventoMissao.Ciente => "2. Ciente",
		EEventoMissao.Deslocando => "3. Deslocando",
		EEventoMissao.NoLocal => "4. No Local",
		EEventoMissao.RemoverPaciente => "5. Remover (Cliente)",
		EEventoMissao.Removendo => "6. Removendo",
		EEventoMissao.NoDestino => "7. No Destino",
		_ => e.ToString(),
	};

	bool CanPublish(string? name)
	{
		if (string.IsNullOrEmpty(name) || !Enum.TryParse<EEventoMissao>(name, out var e))
			return false;
		return _missao.ProximosEventos().Contains(e);
	}

	void OnPublish(string? name)
	{
		if (string.IsNullOrEmpty(name) || !Enum.TryParse<EEventoMissao>(name, out var e))
			return;
		if (e == EEventoMissao.Comunicado)
			_missao.Publish(e, DemoChamadoFactory.Create());
		else
			_missao.Publish(e);
		Refresh();
	}

}

public sealed class SimEventButton
{
	public EEventoMissao Evento { get; set; }
	public string Titulo { get; set; } = "";
	public string EventoName => Evento.ToString();
	public bool IsEnabled { get; set; }
}
