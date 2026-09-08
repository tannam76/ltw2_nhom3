using System.Collections.Concurrent;
using System.Security.Cryptography;
using CourseManagement.DTOs.Auth;
using CourseManagement.Models;

namespace CourseManagement.Services;

public class UserService : IUserService
{
	private readonly ConcurrentDictionary<string, User> usersByEmail = new(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentDictionary<string, User> usersByToken = new(StringComparer.Ordinal);
	private int nextUserId;

	public AuthResponseDto Register(RegisterDto request)
	{
		var email = request.Email.Trim();
		if (usersByEmail.ContainsKey(email))
		{
			throw new InvalidOperationException("Email is already registered.");
		}

		var user = new User
		{
			Id = Interlocked.Increment(ref nextUserId),
			FullName = request.FullName.Trim(),
			Email = email,
			PasswordHash = HashPassword(request.Password)
		};

		if (!usersByEmail.TryAdd(email, user))
		{
			throw new InvalidOperationException("Email is already registered.");
		}

		return CreateResponse(user);
	}

	public AuthResponseDto Login(LoginDto request)
	{
		var email = request.Email.Trim();
		if (!usersByEmail.TryGetValue(email, out var user) || !VerifyPassword(request.Password, user.PasswordHash))
		{
			throw new UnauthorizedAccessException("Invalid email or password.");
		}

		return CreateResponse(user);
	}

	public UserSummaryDto? GetCurrentUser(string? accessToken)
	{
		return accessToken is not null && usersByToken.TryGetValue(accessToken, out var user)
			? ToSummary(user)
			: null;
	}

	private AuthResponseDto CreateResponse(User user)
	{
		var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
		usersByToken[token] = user;

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
