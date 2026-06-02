namespace PrintBridge.Infrastructure.Options;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public bool UseSsl { get; set; } = true;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Supplier Portal";
    public string DefaultSubject{ get; set; } = string.Empty;
    public string DefaultBody{ get; set; } = string.Empty;
}