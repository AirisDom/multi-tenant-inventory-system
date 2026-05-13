using System.ComponentModel.DataAnnotations;

namespace multi_tenant_inventory_system.DTOs;

/// <summary>
/// Request model for tenant and user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Name of the tenant organization
    /// </summary>
    /// <example>Acme Corporation</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string TenantName { get; set; }

    /// <summary>
    /// Email address for the admin user
    /// </summary>
    /// <example>admin@acme.com</example>
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// Password for the admin user (minimum 6 characters)
    /// </summary>
    /// <example>SecurePass123!</example>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string Password { get; set; }
}
