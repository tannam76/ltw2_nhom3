using CourseManagement.Data;
using CourseManagement.DTOs.Product;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    // GET api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll([FromQuery] bool? isActive = null)
    {
        var query = db.Products.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(products);
    }

    // GET api/products/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await db.Products.FindAsync(id);
        return product is null ? NotFound() : Ok(ToDto(product));
    }

    // POST api/products
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            Name        = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Price       = dto.Price,
            ImageUrl    = dto.ImageUrl,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product));
    }

    // PUT api/products/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductDto dto)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.Name        = dto.Name.Trim();
        product.Description = dto.Description.Trim();
        product.Price       = dto.Price;
        product.ImageUrl    = dto.ImageUrl;
        product.IsActive    = dto.IsActive;

        await db.SaveChangesAsync();
        return Ok(ToDto(product));
    }

    // PATCH api/products/{id}/toggle
    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.IsActive = !product.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { product.Id, product.IsActive });
    }

    // DELETE api/products/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id          = p.Id,
        Name        = p.Name,
        Description = p.Description,
        Price       = p.Price,
        ImageUrl    = p.ImageUrl,
        IsActive    = p.IsActive,
        CreatedAt   = p.CreatedAt
    };
}
