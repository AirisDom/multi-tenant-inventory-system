using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_inventory_system.Data;
using multi_tenant_inventory_system.DTOs;
using multi_tenant_inventory_system.Models;
using multi_tenant_inventory_system.Services;

namespace multi_tenant_inventory_system.Controllers;

/// <summary>
/// Manages inventory products for the authenticated tenant
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProductsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="request">The product details</param>
    /// <returns>The created product</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="401">User is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Gets all products for the current tenant
    /// </summary>
    /// <returns>List of products</returns>
    /// <response code="200">Returns the list of products</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>The requested product</returns>
    /// <response code="200">Returns the product</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Product not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="request">The updated product details</param>
    /// <returns>The updated product</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Product not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Product deleted successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Product not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (_tenantContext.TenantId == null)
            return Unauthorized();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
