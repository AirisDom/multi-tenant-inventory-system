namespace multi_tenant_inventory_system.DTOs;

public class LoginResponse
{
    public required string Token { get; set; }
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public Guid TenantId { get; set; }
}
