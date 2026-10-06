using CourseManagement.Data;
using CourseManagement.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(AppDbContext db) : ControllerBase
{
    // GET api/users  – danh sách users (dùng để chọn instructor)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetAll()
    {
        var users = await db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new UserSummaryDto
            {
                Id       = u.Id,
                FullName = u.FullName,
                Email    = u.Email,
                Role     = u.Role
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserSummaryDto>> GetById(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        return Ok(new UserSummaryDto
        {
            Id       = user.Id,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = user.Role
        });
    }
}
