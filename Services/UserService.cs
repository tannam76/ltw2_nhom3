using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CourseManagement.DTOs.Auth;
using CourseManagement.DTOs.Common;
using CourseManagement.DTOs.User;
using CourseManagement.Models;
using CourseManagement.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace CourseManagement.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public UserService(IUserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    public async Task<PagedResult<UserSummaryDto>> GetAllAsync(UserQueryParameters queryParams)
    {
        var users = await _userRepo.GetAllAsync(queryParams);
        var items = users.Items.Select(ToSummary).ToList();

        return new PagedResult<UserSummaryDto>(
            items,
            users.TotalCount,
            users.PageNumber,
            users.PageSize);
    }

    public async Task<UserSummaryDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        return user is null ? null : ToSummary(user);
    }

    public async Task<AuthResponseDto> Register(RegisterDto request)
    {
        var email = request.Email.Trim();
        if (await _userRepo.GetByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = HashPassword(request.Password),
            Role = "Student",
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> Login(LoginDto request)
    {
        var email = request.Email.Trim();
        var user = await _userRepo.GetByEmailAsync(email);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        if (!user.PasswordHash.StartsWith("$2", StringComparison.Ordinal))
        {
            user.PasswordHash = HashPassword(request.Password);
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        return BuildAuthResponse(user);
    }

    public async Task<UserSummaryDto?> GetCurrentUser(string? token)
    {
        var userId = ValidateToken(token);
        if (userId is null)
        {
            return null;
        }

        var user = await _userRepo.GetByIdAsync(userId.Value);
        return user is null ? null : ToSummary(user);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepo.GetAllAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        return user is null ? null : MapToDto(user);
    }

    public async Task<(bool IsSuccess, string Message, UserDto? Data)> CreateAsync(CreateUserDto request)
    {
        var email = request.Email.Trim();
        if (await _userRepo.GetByEmailAsync(email) is not null)
        {
            return (false, "Email đã được sử dụng.", null);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = HashPassword(request.Password),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        return (true, "Tạo người dùng thành công.", MapToDto(user));
    }

    public async Task<(bool IsSuccess, string Message)> UpdateAsync(int id, UpdateUserDto request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null)
        {
            return (false, $"Không tìm thấy người dùng có Id = {id}");
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            user.Role = request.Role;
        }

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        return (true, $"Cập nhật thành công người dùng Id = {id}");
    }

    public async Task<(bool IsSuccess, string Message)> DeleteAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null)
        {
            return (false, $"Không tìm thấy người dùng có Id = {id}");
        }

        _userRepo.Delete(user);
        await _userRepo.SaveChangesAsync();

        return (true, $"Đã xóa người dùng Id = {id}");
    }

    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? "CourseManagement_SuperSecretKey_ForJwtToken_2026!@#$MustBeAtLeast32Bytes";
        var jwtIssuer = _config["Jwt:Issuer"] ?? "CourseManagementAPI";
        var jwtAudience = _config["Jwt:Audience"] ?? "CourseManagementClient";
        var durationHours = int.TryParse(_config["Jwt:DurationInHours"], out var hours) ? hours : 8;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);
        var expires = DateTime.UtcNow.AddHours(durationHours);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AuthResponseDto
        {
            AccessToken = tokenHandler.WriteToken(token),
            ExpiresAt = expires,
            User = ToSummary(user)
        };
    }

    private int? ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var jwtKey = _config["Jwt:Key"] ?? "CourseManagement_SuperSecretKey_ForJwtToken_2026!@#$MustBeAtLeast32Bytes";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "CourseManagementAPI";
            var jwtAudience = _config["Jwt:Audience"] ?? "CourseManagementClient";
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool VerifyPassword(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        if (stored.StartsWith("$2", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, stored);
            }
            catch (Exception)
            {
                return false;
            }
        }

        var parts = stored.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static UserSummaryDto ToSummary(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role
    };

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}
