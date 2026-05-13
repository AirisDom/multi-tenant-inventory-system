using System.ComponentModel.DataAnnotations;

namespace multi_tenant_inventory_system.DTOs;

/// <summary>
/// Request model for updating an existing product
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// Product name
    /// </summary>
    /// <example>Widget Pro Max</example>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; set; }

    /// <summary>
    /// Stock Keeping Unit identifier
    /// </summary>
    /// <example>WGT-PRO-002</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string SKU { get; set; }

    /// <summary>
    /// Current quantity in stock
    /// </summary>
    /// <example>150</example>
    [Range(0, int.MaxValue)]
    public int StockCount { get; set; }
}
