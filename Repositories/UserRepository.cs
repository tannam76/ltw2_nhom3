using CourseManagement.Data;
using CourseManagement.DTOs.Common;
using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    private const int MaxPageSize = 50;

    public async Task<List<User>> GetAllAsync()
    {
        return await db.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ToListAsync();
    }

    public async Task<PagedResult<User>> GetAllAsync(UserQueryParameters queryParams)
    {
        var pageNumber = Math.Max(1, queryParams.PageNumber);
        var pageSize = Math.Clamp(queryParams.PageSize, 1, MaxPageSize);
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = queryParams.SearchTerm.Trim().ToLower();
            query = query.Where(user =>
                user.FullName.ToLower().Contains(term) ||
                user.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Role))
        {
            var role = queryParams.Role.Trim();
            query = query.Where(user => user.Role == role);
        }

        var orderedQuery = queryParams.SortBy?.Trim().ToLowerInvariant() switch
        {
            "email" => queryParams.IsDescending
                ? query.OrderByDescending(user => user.Email)
                : query.OrderBy(user => user.Email),
            "createdat" => queryParams.IsDescending
                ? query.OrderByDescending(user => user.CreatedAt)
                : query.OrderBy(user => user.CreatedAt),
            _ => queryParams.IsDescending
                ? query.OrderByDescending(user => user.FullName)
                : query.OrderBy(user => user.FullName)
        };

        var totalCount = await query.CountAsync();
        var offset = (long)(pageNumber - 1) * pageSize;
        var items = offset >= totalCount
            ? new List<User>()
            : await orderedQuery
                .ThenBy(user => user.Id)
                .Skip((int)offset)
                .Take(pageSize)
                .ToListAsync();

        return new PagedResult<User>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await db.Users.FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await db.Users.FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task AddAsync(User user)
    {
        await db.Users.AddAsync(user);
    }

    public void Update(User user)
    {
        db.Users.Update(user);
    }

    public void Delete(User user)
    {
        db.Users.Remove(user);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await db.SaveChangesAsync() > 0;
    }
}
