using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CourseManagement.DTOs.Auth;
using CourseManagement.Services;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto request)
    {
        var result = await userService.Register(request);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
    {
        var result = await userService.Login(request);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserSummaryDto>> Me()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var userId))
        {
            var userDto = await userService.GetByIdAsync(userId);
            if (userDto != null)
            {
                return Ok(new UserSummaryDto
                {
                    Id = userDto.Id,
                    FullName = userDto.FullName,
                    Email = userDto.Email,
                    Role = userDto.Role
                });
            }
        }

        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var user = await userService.GetCurrentUser(token);
        return user is null ? Unauthorized() : Ok(user);
    }
}
