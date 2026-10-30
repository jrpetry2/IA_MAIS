namespace SaphMovel.Configuration;

public enum RedeConexao
{
	Cabo,
	Wifi,
}

/// <summary>Endereços das APIs (PC na rede). Ao mudar de rede, trocar só <see cref="ConexaoAtual"/>.</summary>
public static class RedeConfig
{
	/// <summary>Rede usada agora pelo PC que corre as APIs.</summary>
	public static readonly RedeConexao ConexaoAtual = RedeConexao.Wifi;

	/// <summary>IPv4 do PC no cabo (<c>ipconfig</c> → adaptador Ethernet).</summary>
	public const string IpCabo = "10.0.2.34";

	/// <summary>IPv4 do PC no Wi-Fi (<c>ipconfig</c> → adaptador Wi-Fi). Muda conforme a rede.</summary>
	public const string IpWifi = "10.0.5.4";

	public const int PortaEventos = 5090;

	public const int PortaStatus = 5088;

	public static string IpAtual => ConexaoAtual == RedeConexao.Wifi ? IpWifi : IpCabo;

	public static string EventosBaseUrl => $"http://{IpAtual}:{PortaEventos}";

	public static string StatusBaseUrl => $"http://{IpAtual}:{PortaStatus}";
}
