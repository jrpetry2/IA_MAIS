using System.Collections.ObjectModel;
using SaphMovel.Models;
using SaphMovel.Services.Status;
using SaphStatus;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel
{
	public ObservableCollection<StatusMenuItem> MenuItems { get; } = new();

	/// <summary>Apenas feedback de UI; sem chamada à API (integração futura).</summary>
	async Task ShowStatusMenuItemClickPlaceholderAsync(StatusMenuItem item)
	{
		const string body =
			"Clique registado. O envio desta ação ao servidor ainda não está ativo nesta versão.";
		await ShowAlertFromAnyThreadAsync(item.Title, body).ConfigureAwait(false);
	}

	void FillMenu()
	{
		MenuItems.Clear();
		MenuItems.Add(new StatusMenuItem { IconFile = "status_sirene.png", Title = "Solicitar Atendimento", IsEmergency = true, StatusType = EStatusType.SolicitarAtendimento });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_pin.png", Title = "Chegada na Base", StatusType = EStatusType.ChegadaNaBase });
		MenuItems.Add(new StatusMenuItem { IconFile = "informar_equipe.png", Title = "Informar Equipe", StatusType = EStatusType.InformarEquipe });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_ambulancia.png", Title = "Troca de Veículo", StatusType = EStatusType.TrocaDeVeiculo });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_combustivel.png", Title = "Solicitar Abastecimento", StatusType = EStatusType.SolicitarAbastecimento });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_higienizacao.png", Title = "Solicitar Higienização", StatusType = EStatusType.SolicitarHigienizacao });
		MenuItems.Add(new StatusMenuItem { IconFile = "status_manutencao.png", Title = "Solicitar Manutenção", StatusType = EStatusType.SolicitarManutencao });
	}

	/// <summary>Reports the selected menu row to the backend via <see cref="IStatusFacade"/>.</summary>
	public Task SubmitStatusMenuAsync(StatusMenuItem item, CancellationToken cancellationToken = default) =>
		_statusFacade.SubmitDeviceStatusAsync(DeviceImei, item.StatusType, cancellationToken);
}
