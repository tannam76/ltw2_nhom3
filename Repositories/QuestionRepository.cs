using CourseManagement.Data;
using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public QuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Questions.AddAsync(question, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task<IReadOnlyList<Question>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .OrderByDescending(q => q.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetByLessonAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Where(q => q.LessonId == lessonId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
