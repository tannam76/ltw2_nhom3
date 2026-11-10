using CourseManagement.DTOs.Common;
using CourseManagement.Models;

namespace CourseManagement.Repositories;

public interface IUserRepository
{
    Task<PagedResult<User>> GetAllAsync(UserQueryParameters queryParams);
}