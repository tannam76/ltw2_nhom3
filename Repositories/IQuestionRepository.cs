using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Data;

public interface IQuestionRepository
{
    Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Question>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Question>> GetByLessonAsync(int lessonId, CancellationToken cancellationToken = default);
}
