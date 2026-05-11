namespace multi_tenant_inventory_system.DTOs;

public class RegisterResponse
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string TenantName { get; set; }
}
