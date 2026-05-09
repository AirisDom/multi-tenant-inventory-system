using Microsoft.EntityFrameworkCore;
using multi_tenant_inventory_system.Models;

namespace multi_tenant_inventory_system.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
}
