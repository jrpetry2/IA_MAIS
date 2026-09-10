namespace SaphStatus;

public sealed class SubmitStatusRequest
{
	public string Imei { get; }
	public EStatusType StatusType { get; }

	public SubmitStatusRequest(string imei, EStatusType statusType)
	{
		Imei = imei;
		StatusType = statusType;
	}
}
