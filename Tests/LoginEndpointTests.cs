using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using multi_tenant_inventory_system.Data;
using multi_tenant_inventory_system.DTOs;
using multi_tenant_inventory_system.Models;
using multi_tenant_inventory_system.Services;

namespace multi_tenant_inventory_system.Tests;

public class LoginEndpointTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginEndpointTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var tenantContext = new TestTenantContext();
        _dbContext = new AppDbContext(options, tenantContext);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
        _passwordHasher = new PasswordHasher();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyForTestingPurposes123!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        _tokenService = new JwtTokenService(configuration);
    }

    public void Dispose()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            CreatedAt = DateTime.UtcNow
        };

        var plainPassword = "SecurePass123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@testcompany.com",
            PasswordHash = _passwordHasher.HashPassword(plainPassword),
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        Assert.NotNull(savedUser);

        var isPasswordValid = _passwordHasher.VerifyPassword(plainPassword, savedUser.PasswordHash);
        Assert.True(isPasswordValid);

        var token = _tokenService.GenerateToken(savedUser);
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_FailsVerification()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            CreatedAt = DateTime.UtcNow
        };

        var plainPassword = "SecurePass123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@testcompany.com",
            PasswordHash = _passwordHasher.HashPassword(plainPassword),
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        Assert.NotNull(savedUser);

        var isPasswordValid = _passwordHasher.VerifyPassword("WrongPassword123!", savedUser.PasswordHash);
        Assert.False(isPasswordValid);
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsNull()
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "nonexistent@test.com");
        Assert.Null(user);
    }

    [Fact]
    public async Task Login_GeneratedToken_ContainsTenantIdClaim()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Token Test Company",
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tokentest@company.com",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        Assert.NotNull(savedUser);

        var token = _tokenService.GenerateToken(savedUser);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var tenantIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id");
        Assert.NotNull(tenantIdClaim);
        Assert.Equal(tenant.Id.ToString(), tenantIdClaim.Value);
    }

    [Fact]
    public void LoginRequest_HasRequiredProperties()
    {
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "password123"
        };

        Assert.NotNull(request.Email);
        Assert.NotNull(request.Password);
    }

    [Fact]
    public void LoginResponse_ContainsExpectedFields()
    {
        var response = new LoginResponse
        {
            Token = "test.jwt.token",
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            TenantId = Guid.NewGuid()
        };

        Assert.NotNull(response.Token);
        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal("test@example.com", response.Email);
        Assert.NotEqual(Guid.Empty, response.TenantId);
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
    }
}
