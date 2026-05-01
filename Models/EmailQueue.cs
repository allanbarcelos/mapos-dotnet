namespace mapos_dotnet.Models;

public class EmailQueue
{
    public int Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "pending"; // pending, sending, sent, failed
    public DateTime? Date { get; set; }
    public string? Headers { get; set; } // JSON
    public string? Subject { get; set; }
}
