using System.Security.Cryptography;
using System.Text;
using CourseManagement.DTOs.User;
using CourseManagement.Models;
using CourseManagement.Repositories;

namespace CourseManagement.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepo.GetAllAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<(bool IsSuccess, string Message, UserDto? Data)> CreateAsync(CreateUserDto request)
    {
        var existingUser = await _userRepo.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, "Email đã được sử dụng.", null);
        }

        var newUser = new User
        {
            FullName = request.FullName,
            Email    = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role      = string.IsNullOrEmpty(request.Role) ? "Student" : request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(newUser);
        await _userRepo.SaveChangesAsync();

        return (true, "Tạo người dùng thành công.", MapToDto(newUser));
    }

    public async Task<(bool IsSuccess, string Message)> UpdateAsync(int id, UpdateUserDto request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
        {
            return (false, $"Không tìm thấy người dùng có Id = {id}");
        }

        if (!string.IsNullOrEmpty(request.FullName)) user.FullName = request.FullName;
        if (!string.IsNullOrEmpty(request.Email))    user.Email    = request.Email;
        if (!string.IsNullOrEmpty(request.Role))     user.Role     = request.Role;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        return (true, $"Cập nhật thành công người dùng Id = {id}");
    }

    public async Task<(bool IsSuccess, string Message)> DeleteAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
        {
            return (false, $"Không tìm thấy người dùng có Id = {id}");
        }

        _userRepo.Delete(user);
        await _userRepo.SaveChangesAsync();

        return (true, $"Đã xóa người dùng Id = {id}");
    }

    // ── Helper Methods ──────────────────────────────────────────────────────────

    private static UserDto MapToDto(User user) => new()
    {
        Id        = user.Id,
        FullName  = user.FullName,
        Email     = user.Email,
        Role      = user.Role,
        CreatedAt = user.CreatedAt
    };

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}
