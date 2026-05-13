namespace multi_tenant_inventory_system.DTOs;

/// <summary>
/// Response model after successful registration
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// Unique identifier of the created tenant
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Unique identifier of the created user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Email address of the registered user
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Name of the registered tenant organization
    /// </summary>
    public required string TenantName { get; set; }
}
