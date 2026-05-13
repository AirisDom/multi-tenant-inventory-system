using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using multi_tenant_inventory_system.Data;
using multi_tenant_inventory_system.DTOs;

namespace multi_tenant_inventory_system.Tests;

public class TenantIsolationIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly SqliteConnection _connection;
    private readonly string _testId;

    public TenantIsolationIntegrationTests()
    {
        _testId = Guid.NewGuid().ToString("N")[..8];
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>((sp, options) =>
                    {
                        options.UseSqlite(_connection);
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                });
            });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    private string UniqueEmail(string prefix) => $"{prefix}_{_testId}@test.com";

    private async Task<(string Token, Guid TenantId)> RegisterAndLoginTenantAsync(string tenantName, string email, string password)
    {
        var registerRequest = new RegisterRequest
        {
            TenantName = tenantName,
            Email = email,
            Password = password
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (loginResult!.Token, loginResult.TenantId);
    }

    private async Task<ProductResponse> CreateProductAsync(string token, CreateProductRequest request)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/products");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        requestMessage.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private async Task<List<ProductResponse>> GetAllProductsAsync(string token)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ProductResponse>>())!;
    }

    private async Task<HttpResponseMessage> GetProductByIdAsync(string token, Guid productId)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/products/{productId}");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(requestMessage);
    }

    private async Task<HttpResponseMessage> UpdateProductAsync(string token, Guid productId, UpdateProductRequest request)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/products/{productId}");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        requestMessage.Content = JsonContent.Create(request);
        return await _client.SendAsync(requestMessage);
    }

    private async Task<HttpResponseMessage> DeleteProductAsync(string token, Guid productId)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{productId}");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(requestMessage);
    }

    [Fact]
    public async Task TenantA_CannotSee_TenantB_Products()
    {
        var (tokenA, tenantIdA) = await RegisterAndLoginTenantAsync("Company A", UniqueEmail("admina"), "Password123!");
        var (tokenB, tenantIdB) = await RegisterAndLoginTenantAsync("Company B", UniqueEmail("adminb"), "Password123!");

        var productA = await CreateProductAsync(tokenA, new CreateProductRequest
        {
            Name = "Product A",
            SKU = "SKU-A-001",
            StockCount = 100
        });

        var productB = await CreateProductAsync(tokenB, new CreateProductRequest
        {
            Name = "Product B",
            SKU = "SKU-B-001",
            StockCount = 200
        });

        var tenantAProducts = await GetAllProductsAsync(tokenA);
        var tenantBProducts = await GetAllProductsAsync(tokenB);

        Assert.Single(tenantAProducts);
        Assert.Equal("Product A", tenantAProducts[0].Name);
        Assert.Equal(tenantIdA, tenantAProducts[0].TenantId);

        Assert.Single(tenantBProducts);
        Assert.Equal("Product B", tenantBProducts[0].Name);
        Assert.Equal(tenantIdB, tenantBProducts[0].TenantId);
    }

    [Fact]
    public async Task TenantA_CannotGetById_TenantB_Product()
    {
        var (tokenA, _) = await RegisterAndLoginTenantAsync("Company A", UniqueEmail("admina"), "Password123!");
        var (tokenB, _) = await RegisterAndLoginTenantAsync("Company B", UniqueEmail("adminb"), "Password123!");

        var productB = await CreateProductAsync(tokenB, new CreateProductRequest
        {
            Name = "Secret Product B",
            SKU = "SKU-SECRET-B",
            StockCount = 50
        });

        var response = await GetProductByIdAsync(tokenA, productB.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantA_CannotUpdate_TenantB_Product()
    {
        var (tokenA, _) = await RegisterAndLoginTenantAsync("Company A", UniqueEmail("admina"), "Password123!");
        var (tokenB, _) = await RegisterAndLoginTenantAsync("Company B", UniqueEmail("adminb"), "Password123!");

        var productB = await CreateProductAsync(tokenB, new CreateProductRequest
        {
            Name = "Original Product B",
            SKU = "SKU-ORIG-B",
            StockCount = 75
        });

        var updateRequest = new UpdateProductRequest
        {
            Name = "Hacked Product Name",
            SKU = "SKU-HACKED",
            StockCount = 999
        };

        var response = await UpdateProductAsync(tokenA, productB.Id, updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var originalProduct = await GetProductByIdAsync(tokenB, productB.Id);
        var productData = await originalProduct.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("Original Product B", productData!.Name);
        Assert.Equal(75, productData.StockCount);
    }

    [Fact]
    public async Task TenantA_CannotDelete_TenantB_Product()
    {
        var (tokenA, _) = await RegisterAndLoginTenantAsync("Company A", UniqueEmail("admina"), "Password123!");
        var (tokenB, _) = await RegisterAndLoginTenantAsync("Company B", UniqueEmail("adminb"), "Password123!");

        var productB = await CreateProductAsync(tokenB, new CreateProductRequest
        {
            Name = "Protected Product B",
            SKU = "SKU-PROT-B",
            StockCount = 30
        });

        var deleteResponse = await DeleteProductAsync(tokenA, productB.Id);

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        var verifyResponse = await GetProductByIdAsync(tokenB, productB.Id);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task TenantA_CanManage_OwnProducts()
    {
        var (tokenA, tenantIdA) = await RegisterAndLoginTenantAsync("Company A", UniqueEmail("admina"), "Password123!");

        var product = await CreateProductAsync(tokenA, new CreateProductRequest
        {
            Name = "My Product",
            SKU = "SKU-MY-001",
            StockCount = 10
        });

        Assert.Equal(tenantIdA, product.TenantId);

        var getResponse = await GetProductByIdAsync(tokenA, product.Id);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await UpdateProductAsync(tokenA, product.Id, new UpdateProductRequest
        {
            Name = "Updated Product",
            SKU = "SKU-UPD-001",
            StockCount = 20
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedProduct = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("Updated Product", updatedProduct!.Name);
        Assert.Equal(20, updatedProduct.StockCount);

        var deleteResponse = await DeleteProductAsync(tokenA, product.Id);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var verifyDeletedResponse = await GetProductByIdAsync(tokenA, product.Id);
        Assert.Equal(HttpStatusCode.NotFound, verifyDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task MultipleProducts_EachTenantSeesOnlyOwn()
    {
        var (tokenA, tenantIdA) = await RegisterAndLoginTenantAsync("Company A", UniqueEmail("admina"), "Password123!");
        var (tokenB, tenantIdB) = await RegisterAndLoginTenantAsync("Company B", UniqueEmail("adminb"), "Password123!");

        await CreateProductAsync(tokenA, new CreateProductRequest { Name = "A-Product-1", SKU = "A-1", StockCount = 1 });
        await CreateProductAsync(tokenA, new CreateProductRequest { Name = "A-Product-2", SKU = "A-2", StockCount = 2 });
        await CreateProductAsync(tokenA, new CreateProductRequest { Name = "A-Product-3", SKU = "A-3", StockCount = 3 });

        await CreateProductAsync(tokenB, new CreateProductRequest { Name = "B-Product-1", SKU = "B-1", StockCount = 10 });
        await CreateProductAsync(tokenB, new CreateProductRequest { Name = "B-Product-2", SKU = "B-2", StockCount = 20 });

        var tenantAProducts = await GetAllProductsAsync(tokenA);
        var tenantBProducts = await GetAllProductsAsync(tokenB);

        Assert.Equal(3, tenantAProducts.Count);
        Assert.All(tenantAProducts, p => Assert.Equal(tenantIdA, p.TenantId));
        Assert.All(tenantAProducts, p => Assert.StartsWith("A-Product", p.Name));

        Assert.Equal(2, tenantBProducts.Count);
        Assert.All(tenantBProducts, p => Assert.Equal(tenantIdB, p.TenantId));
        Assert.All(tenantBProducts, p => Assert.StartsWith("B-Product", p.Name));
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns_Unauthorized()
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        var response = await _client.SendAsync(requestMessage);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Product_TenantId_MatchesAuthenticatedTenant()
    {
        var (token, tenantId) = await RegisterAndLoginTenantAsync("Company A", UniqueEmail("admina"), "Password123!");

        var product = await CreateProductAsync(token, new CreateProductRequest
        {
            Name = "Tenant Verified Product",
            SKU = "TVP-001",
            StockCount = 5
        });

        Assert.Equal(tenantId, product.TenantId);
    }
}
