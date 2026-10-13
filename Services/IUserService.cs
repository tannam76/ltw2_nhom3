using CourseManagement.DTOs.User;

namespace CourseManagement.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<(bool IsSuccess, string Message, UserDto? Data)> CreateAsync(CreateUserDto request);
    Task<(bool IsSuccess, string Message)> UpdateAsync(int id, UpdateUserDto request);
    Task<(bool IsSuccess, string Message)> DeleteAsync(int id);
}
