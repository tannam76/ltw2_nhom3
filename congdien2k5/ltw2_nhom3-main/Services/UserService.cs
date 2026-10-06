using System.Collections.Concurrent;
using System.Security.Cryptography;
using CourseManagement.Data;
using CourseManagement.DTOs.Auth;
using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Services;

public class UserService(AppDbContext db) : IUserService
{
	// Lưu trữ token đơn giản trên bộ nhớ (chỉ dùng cho mục đích demo/học tập)
	// Ánh xạ AccessToken -> UserId
	private static readonly ConcurrentDictionary<string, int> sessions = new(StringComparer.Ordinal);

	public async Task<AuthResponseDto> Register(RegisterDto request)
	{
		var email = request.Email.Trim();
		
		if (await db.Users.AnyAsync(u => u.Email == email))
		{
			throw new InvalidOperationException("Email is already registered.");
		}

		var user = new User
		{
			FullName = request.FullName.Trim(),
			Email = email,
			PasswordHash = HashPassword(request.Password),
			Role = "Student",
			CreatedAt = DateTime.UtcNow
		};

		db.Users.Add(user);
		await db.SaveChangesAsync();

		return CreateResponse(user);
	}

	public async Task<AuthResponseDto> Login(LoginDto request)
	{
		var email = request.Email.Trim();
		var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
		
		if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
		{
			throw new UnauthorizedAccessException("Invalid email or password.");
		}

		return CreateResponse(user);
	}

	public async Task<UserSummaryDto?> GetCurrentUser(string? accessToken)
	{
		if (string.IsNullOrEmpty(accessToken) || !sessions.TryGetValue(accessToken, out var userId))
		{
			return null;
		}

		var user = await db.Users.FindAsync(userId);
		return user != null ? ToSummary(user) : null;
	}

	private AuthResponseDto CreateResponse(User user)
	{
		var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
		sessions[token] = user.Id;

		return new AuthResponseDto
		{
			AccessToken = token,
			ExpiresAt = DateTime.UtcNow.AddHours(8),
			User = ToSummary(user)
		};
	}

	private static UserSummaryDto ToSummary(User user) => new()
	{
		Id = user.Id,
		FullName = user.FullName,
		Email = user.Email,
		Role = user.Role
	};

	private static string HashPassword(string password)
	{
		var salt = RandomNumberGenerator.GetBytes(16);
		var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
		return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
	}

	private static bool VerifyPassword(string password, string storedValue)
	{
		var parts = storedValue.Split(':', 2);
		if (parts.Length != 2)
		{
			return false;
		}

		var salt = Convert.FromBase64String(parts[0]);
		var expectedHash = Convert.FromBase64String(parts[1]);
		var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, expectedHash.Length);
		return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
	}
}
