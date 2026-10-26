namespace SaphMovel.Models;

/// <summary>Item de prescritão (legado: uma linha Nome + qtd + símbolo).</summary>
public sealed class PrescricaoAtivaItem : ObservableModel
{
	bool _pendente = true;
	int _quantidadeConsumida;

	public string Id { get; init; } = string.Empty;

	/// <summary>Metadado opcional do procedimento (não mostrado no card do item).</summary>
	public string TituloCategoria { get; init; } = string.Empty;

	/// <summary>Linha única estilo legado, ex.: "BOMBA INFUSÃO 1 un".</summary>
	public string TextoLinha { get; init; } = string.Empty;

	public bool Pendente
	{
		get => _pendente;
		set
		{
			if (SetField(ref _pendente, value))
				OnPropertyChanged(nameof(Consumido));
		}
	}

	public bool Consumido => !_pendente;

	/// <summary>Quantidade marcada como consumida (por voz ou toque).</summary>
	public int QuantidadeConsumida
	{
		get => _quantidadeConsumida;
		set
		{
			if (SetField(ref _quantidadeConsumida, value))
				OnPropertyChanged(nameof(ConsumidoTexto));
		}
	}

	public string ConsumidoTexto => _quantidadeConsumida > 0
		? $"Consumido ({_quantidadeConsumida} un)"
		: "Consumido";

	/// <summary>Marca o item como consumido, somando a quantidade se já tinha sido consumido antes.</summary>
	public void MarcarConsumido(int quantidade)
	{
		QuantidadeConsumida = (Consumido ? _quantidadeConsumida : 0) + Math.Max(1, quantidade);
		Pendente = false;
	}
}
