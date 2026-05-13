namespace multi_tenant_inventory_system.DTOs;

/// <summary>
/// Response model for product data
/// </summary>
public class ProductResponse
{
    /// <summary>
    /// Unique identifier of the product
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant ID the product belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Product name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Stock Keeping Unit identifier
    /// </summary>
    public required string SKU { get; set; }

    /// <summary>
    /// Current quantity in stock
    /// </summary>
    public int StockCount { get; set; }

    /// <summary>
    /// Timestamp when the product was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the product was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
