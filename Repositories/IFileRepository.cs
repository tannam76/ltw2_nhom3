using CourseManagement.Models;

namespace CourseManagement.Repositories;

public interface IFileRepository
{
    Task<StoredFile> AddAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredFile>> AddRangeAsync(IEnumerable<StoredFile> files, CancellationToken cancellationToken = default);
    Task<StoredFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StoredFile?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken = default);
}
