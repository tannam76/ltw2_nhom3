using CourseManagement.DTOs.Auth;
using CourseManagement.DTOs.Common;

namespace CourseManagement.Services;

public interface IUserService
{
	Task<PagedResult<UserSummaryDto>> GetAllAsync(UserQueryParameters queryParams);
	Task<UserSummaryDto?> GetUserByIdAsync(int id);
	Task<AuthResponseDto> Register(RegisterDto request);
	Task<AuthResponseDto> Login(LoginDto request);
	Task<UserSummaryDto?> GetCurrentUser(string? accessToken);
}
