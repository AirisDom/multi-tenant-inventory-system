namespace multi_tenant_inventory_system.Services;

public interface ITenantContext
{
    Guid? TenantId { get; set; }
}
