namespace multi_tenant_inventory_system.DTOs;

/// <summary>
/// Response model after successful authentication
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT token for API authorization
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Unique identifier of the authenticated user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Email address of the authenticated user
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Tenant ID the user belongs to
    /// </summary>
    public Guid TenantId { get; set; }
}
