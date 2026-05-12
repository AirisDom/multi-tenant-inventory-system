namespace multi_tenant_inventory_system.DTOs;

public class ProductResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string SKU { get; set; }
    public int StockCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
