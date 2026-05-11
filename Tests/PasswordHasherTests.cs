using multi_tenant_inventory_system.Services;

namespace multi_tenant_inventory_system.Tests;

public class PasswordHasherTests
{
    private readonly IPasswordHasher _passwordHasher = new PasswordHasher();

    [Fact]
    public void HashPassword_ReturnsNonEmptyString()
    {
        var password = "MySecurePassword123!";

        var hash = _passwordHasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ReturnsDifferentHashForSamePassword()
    {
        var password = "MySecurePassword123!";

        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_ReturnsBcryptFormattedHash()
    {
        var password = "MySecurePassword123!";

        var hash = _passwordHasher.HashPassword(password);

        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        var password = "MySecurePassword123!";
        var hash = _passwordHasher.HashPassword(password);

        var result = _passwordHasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _passwordHasher.HashPassword(password);

        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForEmptyPassword()
    {
        var password = "MySecurePassword123!";
        var hash = _passwordHasher.HashPassword(password);

        var result = _passwordHasher.VerifyPassword("", hash);

        Assert.False(result);
    }

    [Fact]
    public void HashPassword_WorksWithEmptyString()
    {
        var password = "";

        var hash = _passwordHasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.True(_passwordHasher.VerifyPassword(password, hash));
    }

    [Fact]
    public void HashPassword_WorksWithSpecialCharacters()
    {
        var password = "P@$$w0rd!#%^&*()_+-=[]{}|;':\",./<>?`~";

        var hash = _passwordHasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.True(_passwordHasher.VerifyPassword(password, hash));
    }

    [Fact]
    public void HashPassword_WorksWithUnicodeCharacters()
    {
        var password = "密码пароль🔐";

        var hash = _passwordHasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.True(_passwordHasher.VerifyPassword(password, hash));
    }
}
