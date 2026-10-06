using CourseManagement.DTOs.Auth;

namespace CourseManagement.Services;

public interface IUserService
{
	Task<AuthResponseDto> Register(RegisterDto request);
	Task<AuthResponseDto> Login(LoginDto request);
	Task<UserSummaryDto?> GetCurrentUser(string? accessToken);
}
