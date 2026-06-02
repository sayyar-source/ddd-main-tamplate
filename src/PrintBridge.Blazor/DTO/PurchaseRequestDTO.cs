namespace PrintBridge.Blazor.DTO;

public class PurchaseRequestDTO
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierTitle { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseRequestItemDTO> Items { get; set; } = new();
}
