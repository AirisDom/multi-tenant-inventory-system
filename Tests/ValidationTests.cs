using System.ComponentModel.DataAnnotations;
using multi_tenant_inventory_system.DTOs;

namespace multi_tenant_inventory_system.Tests;

public class ValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void RegisterRequest_ValidData_PassesValidation()
    {
        var request = new RegisterRequest
        {
            TenantName = "Test Company",
            Email = "test@example.com",
            Password = "password123"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void RegisterRequest_EmptyTenantName_FailsValidation()
    {
        var request = new RegisterRequest
        {
            TenantName = "",
            Email = "test@example.com",
            Password = "password123"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("TenantName"));
    }

    [Fact]
    public void RegisterRequest_TenantNameTooLong_FailsValidation()
    {
        var request = new RegisterRequest
        {
            TenantName = new string('a', 101),
            Email = "test@example.com",
            Password = "password123"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("TenantName"));
    }

    [Fact]
    public void RegisterRequest_InvalidEmail_FailsValidation()
    {
        var request = new RegisterRequest
        {
            TenantName = "Test Company",
            Email = "invalid-email",
            Password = "password123"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void RegisterRequest_PasswordTooShort_FailsValidation()
    {
        var request = new RegisterRequest
        {
            TenantName = "Test Company",
            Email = "test@example.com",
            Password = "12345"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void RegisterRequest_PasswordTooLong_FailsValidation()
    {
        var request = new RegisterRequest
        {
            TenantName = "Test Company",
            Email = "test@example.com",
            Password = new string('a', 101)
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void LoginRequest_ValidData_PassesValidation()
    {
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void LoginRequest_InvalidEmail_FailsValidation()
    {
        var request = new LoginRequest
        {
            Email = "not-an-email",
            Password = "password123"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void LoginRequest_EmptyPassword_FailsValidation()
    {
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = ""
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void CreateProductRequest_ValidData_PassesValidation()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = "SKU-001",
            StockCount = 100
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void CreateProductRequest_EmptyName_FailsValidation()
    {
        var request = new CreateProductRequest
        {
            Name = "",
            SKU = "SKU-001",
            StockCount = 100
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void CreateProductRequest_NameTooLong_FailsValidation()
    {
        var request = new CreateProductRequest
        {
            Name = new string('a', 201),
            SKU = "SKU-001",
            StockCount = 100
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void CreateProductRequest_EmptySKU_FailsValidation()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = "",
            StockCount = 100
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("SKU"));
    }

    [Fact]
    public void CreateProductRequest_SKUTooLong_FailsValidation()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = new string('a', 101),
            StockCount = 100
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("SKU"));
    }

    [Fact]
    public void CreateProductRequest_NegativeStockCount_FailsValidation()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = "SKU-001",
            StockCount = -1
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("StockCount"));
    }

    [Fact]
    public void CreateProductRequest_ZeroStockCount_PassesValidation()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = "SKU-001",
            StockCount = 0
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void UpdateProductRequest_ValidData_PassesValidation()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            SKU = "SKU-002",
            StockCount = 50
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void UpdateProductRequest_EmptyName_FailsValidation()
    {
        var request = new UpdateProductRequest
        {
            Name = "",
            SKU = "SKU-002",
            StockCount = 50
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void UpdateProductRequest_NameTooLong_FailsValidation()
    {
        var request = new UpdateProductRequest
        {
            Name = new string('a', 201),
            SKU = "SKU-002",
            StockCount = 50
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void UpdateProductRequest_EmptySKU_FailsValidation()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            SKU = "",
            StockCount = 50
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("SKU"));
    }

    [Fact]
    public void UpdateProductRequest_SKUTooLong_FailsValidation()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            SKU = new string('a', 101),
            StockCount = 50
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("SKU"));
    }

    [Fact]
    public void UpdateProductRequest_NegativeStockCount_FailsValidation()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            SKU = "SKU-002",
            StockCount = -10
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("StockCount"));
    }

    [Fact]
    public void UpdateProductRequest_ZeroStockCount_PassesValidation()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            SKU = "SKU-002",
            StockCount = 0
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }
}
