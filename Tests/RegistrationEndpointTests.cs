using Microsoft.EntityFrameworkCore;
using multi_tenant_inventory_system.Data;
using multi_tenant_inventory_system.DTOs;
using multi_tenant_inventory_system.Models;
using multi_tenant_inventory_system.Services;

namespace multi_tenant_inventory_system.Tests;

public class RegistrationEndpointTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public RegistrationEndpointTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var tenantContext = new TestTenantContext();
        _dbContext = new AppDbContext(options, tenantContext);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
        _passwordHasher = new PasswordHasher();
    }

    public void Dispose()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Register_CreatesTenantAndUser_InTransaction()
    {
        var request = new RegisterRequest
        {
            TenantName = "Test Company",
            Email = "admin@testcompany.com",
            Password = "SecurePass123!"
        };

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.TenantName,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var savedTenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);
        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        Assert.NotNull(savedTenant);
        Assert.NotNull(savedUser);
        Assert.Equal(request.TenantName, savedTenant.Name);
        Assert.Equal(request.Email, savedUser.Email);
        Assert.Equal("TenantAdmin", savedUser.Role);
        Assert.Equal(tenant.Id, savedUser.TenantId);
    }

    [Fact]
    public async Task Register_UserHasCorrectTenantId()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Another Company",
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@another.com",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var savedUser = await _dbContext.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == "user@another.com");

        Assert.NotNull(savedUser);
        Assert.NotNull(savedUser.Tenant);
        Assert.Equal(tenant.Id, savedUser.TenantId);
        Assert.Equal("Another Company", savedUser.Tenant.Name);
    }

    [Fact]
    public async Task Register_PasswordIsHashed()
    {
        var plainPassword = "MyPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(plainPassword);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Hash Test Company",
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "hashtest@company.com",
            PasswordHash = hashedPassword,
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "hashtest@company.com");

        Assert.NotNull(savedUser);
        Assert.NotEqual(plainPassword, savedUser.PasswordHash);
        Assert.True(_passwordHasher.VerifyPassword(plainPassword, savedUser.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsException()
    {
        var tenant1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Company 1",
            CreatedAt = DateTime.UtcNow
        };

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@test.com",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            TenantId = tenant1.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant1);
        _dbContext.Users.Add(user1);
        await _dbContext.SaveChangesAsync();

        var tenant2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Company 2",
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@test.com",
            PasswordHash = _passwordHasher.HashPassword("password456"),
            TenantId = tenant2.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant2);
        _dbContext.Users.Add(user2);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Register_TransactionRollback_NothingPersisted()
    {
        var initialTenantCount = await _dbContext.Tenants.CountAsync();
        var initialUserCount = await _dbContext.Users.CountAsync();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Rollback Test",
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "rollback@test.com",
            PasswordHash = _passwordHasher.HashPassword("password"),
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        await transaction.RollbackAsync();

        _dbContext.ChangeTracker.Clear();

        var finalTenantCount = await _dbContext.Tenants.CountAsync();
        var finalUserCount = await _dbContext.Users.CountAsync();

        Assert.Equal(initialTenantCount, finalTenantCount);
        Assert.Equal(initialUserCount, finalUserCount);
    }

    [Fact]
    public void RegisterRequest_RequiredFields()
    {
        var request = new RegisterRequest
        {
            TenantName = "Test",
            Email = "test@test.com",
            Password = "password123"
        };

        Assert.NotNull(request.TenantName);
        Assert.NotNull(request.Email);
        Assert.NotNull(request.Password);
    }

    [Fact]
    public void RegisterResponse_ContainsExpectedFields()
    {
        var response = new RegisterResponse
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            TenantName = "Test Company"
        };

        Assert.NotEqual(Guid.Empty, response.TenantId);
        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal("test@example.com", response.Email);
        Assert.Equal("Test Company", response.TenantName);
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
    }
}
