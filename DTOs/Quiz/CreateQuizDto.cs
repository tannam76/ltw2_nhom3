using System.ComponentModel.DataAnnotations;

namespace CourseManagement.DTOs.Quiz;

public class CreateQuizDto
{
    [Required]
    public int LessonId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0, 100)]
    public int PassingScore { get; set; } = 50;

    [MinLength(1)]
    public List<CreateQuizQuestionDto> Questions { get; set; } = [];
}

public class CreateQuizQuestionDto
{
    [Required, StringLength(1000)]
    public string Question { get; set; } = string.Empty;

    [MinLength(2)]
    public List<CreateQuizOptionDto> Options { get; set; } = [];

    [Range(0, int.MaxValue)]
    public int CorrectOptionIndex { get; set; }
}

public class CreateQuizOptionDto
{
    [Required, StringLength(500)]
    public string Text { get; set; } = string.Empty;
}
