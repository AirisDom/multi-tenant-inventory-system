using System.ComponentModel.DataAnnotations;

namespace multi_tenant_inventory_system.DTOs;

/// <summary>
/// Request model for creating a new product
/// </summary>
public class CreateProductRequest
{
    /// <summary>
    /// Product name
    /// </summary>
    /// <example>Widget Pro</example>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; set; }

    /// <summary>
    /// Stock Keeping Unit identifier
    /// </summary>
    /// <example>WGT-PRO-001</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string SKU { get; set; }

    /// <summary>
    /// Current quantity in stock
    /// </summary>
    /// <example>100</example>
    [Range(0, int.MaxValue)]
    public int StockCount { get; set; }
}
