namespace multi_tenant_inventory_system.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
