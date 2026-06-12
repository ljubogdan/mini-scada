namespace Shared.DTOs;

public class SecureMessageDto
{
    public Guid SensorId { get; set; }
    public string EncryptedPayload { get; set; } = string.Empty;
    public string IV { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
