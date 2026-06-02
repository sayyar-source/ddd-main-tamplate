namespace PrintBridge.Blazor.DTO;

public class PurchaseRequestItemDTO
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsPriced { get; set; }
}
