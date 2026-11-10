using CourseManagement.Data;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    // GET api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var categories = await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.CreatedAt })
            .ToListAsync();

        return Ok(categories);
    }

    // GET api/categories/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await db.Categories.FindAsync(id);
        return category is null ? NotFound() : Ok(new { category.Id, category.Name, category.CreatedAt });
    }

    // POST api/categories
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tên danh mục không được để trống." });

        var exists = await db.Categories.AnyAsync(c => c.Name == request.Name.Trim());
        if (exists)
            return Conflict(new { message = "Danh mục đã tồn tại." });

        var category = new Category { Name = request.Name.Trim(), CreatedAt = DateTime.UtcNow };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new { category.Id, category.Name, category.CreatedAt });
    }

    // PUT api/categories/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tên danh mục không được để trống." });

        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        var duplicate = await db.Categories.AnyAsync(c => c.Name == request.Name.Trim() && c.Id != id);
        if (duplicate)
            return Conflict(new { message = "Tên danh mục đã được sử dụng." });

        category.Name = request.Name.Trim();
        await db.SaveChangesAsync();

        return Ok(new { category.Id, category.Name, category.CreatedAt });
    }

    // DELETE api/categories/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record CategoryRequest(string Name);