using CourseManagement.Data;
using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CourseManagement.Repositories;

public class FileRepository : IFileRepository
{
    private readonly AppDbContext _context;

    public FileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StoredFile> AddAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        await _context.StoredFiles.AddAsync(file, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return file;
    }

    public async Task<IReadOnlyList<StoredFile>> AddRangeAsync(
        IEnumerable<StoredFile> files,
        CancellationToken cancellationToken = default)
    {
        var list = files.ToList();
        await _context.StoredFiles.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return list;
    }

    public async Task<StoredFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.StoredFiles.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<StoredFile?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return await _context.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FileName == fileName, cancellationToken);
    }
}
