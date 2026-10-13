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
        try
        {
            return Ok(await userService.Register(request));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
    {
        try
        {
            return Ok(await userService.Login(request));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
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
