namespace multi_tenant_inventory_system.Services;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; set; }
}
