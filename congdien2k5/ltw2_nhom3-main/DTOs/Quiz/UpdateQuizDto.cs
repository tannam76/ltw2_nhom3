using System.ComponentModel.DataAnnotations;

namespace CourseManagement.DTOs.Quiz;

public class UpdateQuizDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0, 100)]
    public int PassingScore { get; set; } = 50;

    [MinLength(1)]
    public List<CreateQuizQuestionDto> Questions { get; set; } = [];
}
