using CourseManagement.Data;
using CourseManagement.DTOs.Auth;
using CourseManagement.DTOs.Common;
using CourseManagement.DTOs.User;
using CourseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> GetAll(
        [FromQuery] UserQueryParameters queryParams)
    {
        var users = await userService.GetAllAsync(queryParams);
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null
            ? NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" })
            : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto request)
    {
        var result = await userService.CreateAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto request)
    {
        var result = await userService.UpdateAsync(id, request);
        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await userService.DeleteAsync(id);
        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedUsers([FromQuery] int count = 100, [FromServices] AppDbContext db = null!)
    {
        var seededCount = await DataSeeder.SeedUsersAsync(db, count);
        return Ok(new
        {
            message = seededCount > 0
                ? $"Đã tạo thành công {seededCount} người dùng mới."
                : $"Cơ sở dữ liệu đã có đủ {count} người dùng trở lên.",
            seededCount
        });
    }
}
