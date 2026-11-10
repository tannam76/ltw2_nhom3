using CourseManagement.DTOs.Auth;
using CourseManagement.DTOs.Common;
using CourseManagement.DTOs.User;

namespace CourseManagement.Services;

public interface IUserService
{
    Task<PagedResult<UserSummaryDto>> GetAllAsync(UserQueryParameters queryParams);
    Task<UserSummaryDto?> GetUserByIdAsync(int id);
    Task<AuthResponseDto> Register(RegisterDto request);
    Task<AuthResponseDto> Login(LoginDto request);
    Task<UserSummaryDto?> GetCurrentUser(string? token);

    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<(bool IsSuccess, string Message, UserDto? Data)> CreateAsync(CreateUserDto request);
    Task<(bool IsSuccess, string Message)> UpdateAsync(int id, UpdateUserDto request);
    Task<(bool IsSuccess, string Message)> DeleteAsync(int id);
}
