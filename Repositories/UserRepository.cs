using Microsoft.EntityFrameworkCore;
using CourseManagement.DTOs.Common;
using CourseManagement.Data;
using CourseManagement.Models;

namespace CourseManagement.Repositories;

public class UserRepository : IUserRepository
{
    private const int MaxPageSize = 50;
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<User>> GetAllAsync(UserQueryParameters queryParams)
    {
        var pageNumber = Math.Max(1, queryParams.PageNumber);
        var pageSize = Math.Clamp(queryParams.PageSize, 1, MaxPageSize);
        var query = _db.Users.AsNoTracking().AsQueryable();

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
}
