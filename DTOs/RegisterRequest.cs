using System.ComponentModel.DataAnnotations;

namespace multi_tenant_inventory_system.DTOs;

public class RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string TenantName { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string Password { get; set; }
}
