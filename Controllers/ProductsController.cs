using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_inventory_system.Data;
using multi_tenant_inventory_system.DTOs;
using multi_tenant_inventory_system.Models;
using multi_tenant_inventory_system.Services;

namespace multi_tenant_inventory_system.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProductsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request)
    {
        if (_tenantContext.TenantId == null)
            return Unauthorized();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId.Value,
            Name = request.Name,
            SKU = request.SKU,
            StockCount = request.StockCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var response = new ProductResponse
        {
            Id = product.Id,
            TenantId = product.TenantId,
            Name = product.Name,
            SKU = product.SKU,
            StockCount = product.StockCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return CreatedAtAction(nameof(Create), new { id = product.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
    {
        if (_tenantContext.TenantId == null)
            return Unauthorized();

        var products = await _db.Products
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                TenantId = p.TenantId,
                Name = p.Name,
                SKU = p.SKU,
                StockCount = p.StockCount,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id)
    {
        if (_tenantContext.TenantId == null)
            return Unauthorized();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        return Ok(new ProductResponse
        {
            Id = product.Id,
            TenantId = product.TenantId,
            Name = product.Name,
            SKU = product.SKU,
            StockCount = product.StockCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, UpdateProductRequest request)
    {
        if (_tenantContext.TenantId == null)
            return Unauthorized();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        product.Name = request.Name;
        product.SKU = request.SKU;
        product.StockCount = request.StockCount;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new ProductResponse
        {
            Id = product.Id,
            TenantId = product.TenantId,
            Name = product.Name,
            SKU = product.SKU,
            StockCount = product.StockCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        });
    }
}
