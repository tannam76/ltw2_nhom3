using CourseManagement.DTOs.Auth;
using CourseManagement.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using CourseManagement.Services;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    // GET api/users  – danh sách users (dùng để chọn instructor)
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> GetAll(
        [FromQuery] UserQueryParameters queryParams)
    {
        var result = await userService.GetAllAsync(queryParams);
        return Ok(result);
    }

    // GET api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserSummaryDto>> GetById(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }
}
