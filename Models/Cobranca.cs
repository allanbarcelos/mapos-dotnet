namespace mapos_dotnet.Models;

public class Cobranca
{
    public int Id { get; set; }
    public string? ChargeId { get; set; }
    public DateOnly? ConditionalDiscountDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CustomId { get; set; }
    public DateOnly ExpireAt { get; set; }
    public string? Message { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentUrl { get; set; }
    public string? RequestDeliveryAddress { get; set; }
    public string Status { get; set; } = "pending";
    public decimal? Total { get; set; }
    public string? Barcode { get; set; }
    public string? Link { get; set; }
    public string? PaymentGateway { get; set; }
    public string? Payment { get; set; }
    public string? Pdf { get; set; }
    public int? VendaId { get; set; }
    public int? OsId { get; set; }
    public int? ClienteId { get; set; }

    public Venda? Venda { get; set; }
    public Os? Os { get; set; }
    public Cliente? Cliente { get; set; }
}
