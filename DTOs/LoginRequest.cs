using System.ComponentModel.DataAnnotations;

namespace multi_tenant_inventory_system.DTOs;

/// <summary>
/// Request model for user authentication
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    /// <example>admin@acme.com</example>
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// User's password
    /// </summary>
    /// <example>SecurePass123!</example>
    [Required]
    public required string Password { get; set; }
}
