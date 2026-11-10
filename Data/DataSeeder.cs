using CourseManagement.Models;
using CourseManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Data;

public static class DataSeeder
{
    private static readonly string[] FirstNames = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương"];
    private static readonly string[] MiddleNames = ["Văn", "Thị", "Hữu", "Đức", "Minh", "Thanh", "Hoàng", "Quốc", "Gia", "Hải", "Tuấn", "Ngọc", "Xuân", "Thành"];
    private static readonly string[] LastNames = ["Anh", "Bình", "Châu", "Dũng", "Em", "Giang", "Hải", "Khánh", "Linh", "Minh", "Nam", "Phúc", "Quân", "Sơn", "Tâm", "Thắng", "Trang", "Tùng", "Vinh", "Yến"];

    public static async Task<int> SeedUsersAsync(AppDbContext context, int count = 100)
    {
        var existingCount = await context.Users.CountAsync();
        if (existingCount >= count)
        {
            return 0; // Đã có đủ dữ liệu
        }

        var needed = count - existingCount;
        var existingEmails = (await context.Users.Select(u => u.Email).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Tạo sẵn hash một lần cho toàn bộ 100 user để tối ưu hiệu năng
        var defaultPasswordHash = UserService.HashPassword("Password123@");
        var usersToAdd = new List<User>();
        var random = new Random(42); // Seed cố định để dữ liệu nhất quán

        int index = 1;
        while (usersToAdd.Count < needed && index <= 500)
        {
            var email = $"user{index:D3}@example.com";
            if (!existingEmails.Contains(email))
            {
                var firstName = FirstNames[random.Next(FirstNames.Length)];
                var middleName = MiddleNames[random.Next(MiddleNames.Length)];
                var lastName = LastNames[random.Next(LastNames.Length)];
                var fullName = $"{firstName} {middleName} {lastName}";

                // Phân chia Role: 80% Student, 15% Instructor, 5% Admin
                string role = (index % 20) switch
                {
                    0 => "Admin",
                    1 or 2 or 3 => "Instructor",
                    _ => "Student"
                };

                // Trải đều CreatedAt trong 120 ngày gần nhất
                var daysAgo = random.Next(0, 120);
                var createdAt = DateTime.UtcNow.AddDays(-daysAgo).AddMinutes(random.Next(0, 1440));

                usersToAdd.Add(new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = defaultPasswordHash,
                    Role = role,
                    CreatedAt = createdAt
                });

                existingEmails.Add(email);
            }
            index++;
        }

        if (usersToAdd.Count > 0)
        {
            await context.Users.AddRangeAsync(usersToAdd);
            await context.SaveChangesAsync();
        }

        return usersToAdd.Count;
    }
}
