using CourseManagement.DTOs.Auth;

namespace CourseManagement.Services;

public interface IUserService
{
	AuthResponseDto Register(RegisterDto request);
	AuthResponseDto Login(LoginDto request);
	UserSummaryDto? GetCurrentUser(string? accessToken);
}
