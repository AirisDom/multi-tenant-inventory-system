using Microsoft.EntityFrameworkCore;

namespace multi_tenant_inventory_system.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
