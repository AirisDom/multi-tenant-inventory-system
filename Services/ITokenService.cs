using multi_tenant_inventory_system.Models;

namespace multi_tenant_inventory_system.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
