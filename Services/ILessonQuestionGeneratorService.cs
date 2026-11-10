using CourseManagement.Models;

namespace CourseManagement.Services;

public interface ILessonQuestionGeneratorService
{
    /// <summary>
    /// Phân tích nội dung và tiêu đề của bài học để tự động sinh ra các câu hỏi (tự luận và/hoặc trắc nghiệm)
    /// </summary>
    /// <param name="lessonId">ID bài học</param>
    /// <param name="lessonTitle">Tiêu đề bài học</param>
    /// <param name="lessonContent">Nội dung bài học đưa vào</param>
    /// <param name="creatorId">ID người tạo bài học</param>
    /// <param name="count">Số lượng câu hỏi cần sinh (mặc định 3)</param>
    /// <param name="preferredType">Loại câu hỏi: "Both", "Essay", "MultipleChoice"</param>
    /// <returns>Danh sách các đối tượng LessonQuestion (kèm Options nếu là trắc nghiệm)</returns>
    List<LessonQuestion> GenerateQuestionsFromContent(
        int lessonId,
        string lessonTitle,
        string lessonContent,
        int creatorId,
        int count = 3,
        string preferredType = "Both");
}
