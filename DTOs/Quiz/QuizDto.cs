namespace CourseManagement.DTOs.Quiz;

public class QuizDto
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public IReadOnlyList<QuizQuestionDto> Questions { get; set; } = [];
}

public class QuizQuestionDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public IReadOnlyList<QuizOptionDto> Options { get; set; } = [];
}

public class QuizOptionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
