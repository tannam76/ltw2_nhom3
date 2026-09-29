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

	[HttpGet("me")]
	public async Task<ActionResult<UserSummaryDto>> Me()
	{
		var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
		var user = await userService.GetCurrentUser(token);
		return user is null ? Unauthorized() : Ok(user);
	}
}
