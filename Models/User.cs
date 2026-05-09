namespace multi_tenant_inventory_system.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public Guid TenantId { get; set; }
    public required string Role { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
