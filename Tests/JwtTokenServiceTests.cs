using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using multi_tenant_inventory_system.Models;
using multi_tenant_inventory_system.Services;

namespace multi_tenant_inventory_system.Tests;

public class JwtTokenServiceTests
{
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceTests()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "TestSecretKeyThatIsLongEnoughForHmacSha256Algorithm123!" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" },
            { "Jwt:ExpirationMinutes", "30" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _tokenService = new JwtTokenService(_configuration);
    }

    private User CreateTestUser() => new User
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        PasswordHash = "hashedpassword",
        TenantId = Guid.NewGuid(),
        Role = "TenantAdmin"
    };

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwtFormat()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateToken_ContainsUserIdClaim()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

        Assert.NotNull(subClaim);
        Assert.Equal(user.Id.ToString(), subClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);

        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsTenantIdClaim()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var tenantClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id");

        Assert.NotNull(tenantClaim);
        Assert.Equal(user.TenantId.ToString(), tenantClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsRoleClaim()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        Assert.NotNull(roleClaim);
        Assert.Equal(user.Role, roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuer()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("test-issuer", jwtToken.Issuer);
    }

    [Fact]
    public void GenerateToken_HasCorrectAudience()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Contains("test-audience", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_HasFutureExpiration()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ExpiresWithinConfiguredTime()
    {
        var user = CreateTestUser();

        var token = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = DateTime.UtcNow.AddMinutes(30);
        var tolerance = TimeSpan.FromSeconds(30);

        Assert.True(Math.Abs((jwtToken.ValidTo - expectedExpiration).TotalSeconds) < tolerance.TotalSeconds);
    }

    [Fact]
    public void GenerateToken_GeneratesUniqueJtiForEachToken()
    {
        var user = CreateTestUser();

        var token1 = _tokenService.GenerateToken(user);
        var token2 = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken1 = handler.ReadJwtToken(token1);
        var jwtToken2 = handler.ReadJwtToken(token2);

        var jti1 = jwtToken1.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        var jti2 = jwtToken2.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        Assert.NotNull(jti1);
        Assert.NotNull(jti2);
        Assert.NotEqual(jti1, jti2);
    }

    [Fact]
    public void GenerateToken_ThrowsWhenSecretKeyNotConfigured()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var tokenService = new JwtTokenService(configuration);
        var user = CreateTestUser();

        Assert.Throws<InvalidOperationException>(() => tokenService.GenerateToken(user));
    }
}
