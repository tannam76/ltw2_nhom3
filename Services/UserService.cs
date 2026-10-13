using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CourseManagement.DTOs.Auth;
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

    // ── Auth ────────────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> Register(RegisterDto request)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email đã được sử dụng.");

        var user = new User
        {
            FullName     = request.FullName,
            Email        = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role         = "Student",
            CreatedAt    = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> Login(LoginDto request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        // Nếu mật khẩu cũ là định dạng hash cũ (PBKDF2), tự động nâng cấp lên BCrypt
        if (!user.PasswordHash.StartsWith("$2"))
        {
            user.PasswordHash = HashPassword(request.Password);
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        return BuildAuthResponse(user);
    }

    public async Task<UserSummaryDto?> GetCurrentUser(string token)
    {
        var userId = ValidateToken(token);
        if (userId == null) return null;

        var user = await _userRepo.GetByIdAsync(userId.Value);
        if (user == null) return null;

        return new UserSummaryDto
        {
            Id       = user.Id,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = user.Role
        };
    }

    // ── CRUD ────────────────────────────────────────────────────────────────────

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
            return (false, "Email đã được sử dụng.", null);

        var newUser = new User
        {
            FullName     = request.FullName,
            Email        = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role         = string.IsNullOrEmpty(request.Role) ? "Student" : request.Role,
            CreatedAt    = DateTime.UtcNow
        };

        await _userRepo.AddAsync(newUser);
        await _userRepo.SaveChangesAsync();

        return (true, "Tạo người dùng thành công.", MapToDto(newUser));
    }

    public async Task<(bool IsSuccess, string Message)> UpdateAsync(int id, UpdateUserDto request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return (false, $"Không tìm thấy người dùng có Id = {id}");

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
            return (false, $"Không tìm thấy người dùng có Id = {id}");

        _userRepo.Delete(user);
        await _userRepo.SaveChangesAsync();

        return (true, $"Đã xóa người dùng Id = {id}");
    }

    // ── JWT Token Helpers ───────────────────────────────────────────────────────

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? "CourseManagement_SuperSecretKey_ForJwtToken_2026!@#$MustBeAtLeast32Bytes";
        var jwtIssuer = _config["Jwt:Issuer"] ?? "CourseManagementAPI";
        var jwtAudience = _config["Jwt:Audience"] ?? "CourseManagementClient";
        var durationHours = int.TryParse(_config["Jwt:DurationInHours"], out var h) ? h : 8;

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
        var tokenString = tokenHandler.WriteToken(token);

        return new AuthResponseDto
        {
            AccessToken = tokenString,
            ExpiresAt   = expires,
            User        = new UserSummaryDto
            {
                Id       = user.Id,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = user.Role
            }
        };
    }

    private int? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            var jwtKey = _config["Jwt:Key"] ?? "CourseManagement_SuperSecretKey_ForJwtToken_2026!@#$MustBeAtLeast32Bytes";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "CourseManagementAPI";
            var jwtAudience = _config["Jwt:Audience"] ?? "CourseManagementClient";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
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
        catch
        {
            return null;
        }
    }

    // ── BCrypt Password Helpers ─────────────────────────────────────────────────

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return false;

        // Nếu là định dạng BCrypt ($2a$, $2b$, $2y$)
        if (stored.StartsWith("$2"))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, stored);
            }
            catch
            {
                return false;
            }
        }

        // Tương thích ngược với định dạng cũ (PBKDF2 salt:hash)
        var parts = stored.Split(':', 2);
        if (parts.Length != 2) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100_000, HashAlgorithmName.SHA256, 32);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id        = user.Id,
        FullName  = user.FullName,
        Email     = user.Email,
        Role      = user.Role,
        CreatedAt = user.CreatedAt
    };
}
