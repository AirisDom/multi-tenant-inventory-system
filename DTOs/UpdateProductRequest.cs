using System.ComponentModel.DataAnnotations;

namespace multi_tenant_inventory_system.DTOs;

public class UpdateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string SKU { get; set; }

    [Range(0, int.MaxValue)]
    public int StockCount { get; set; }
}
