using System.Text.RegularExpressions;
using CourseManagement.Models;

namespace CourseManagement.Services;

public class LessonQuestionGeneratorService : ILessonQuestionGeneratorService
{
    private static readonly Random Rnd = new();

    public List<LessonQuestion> GenerateQuestionsFromContent(
        int lessonId,
        string lessonTitle,
        string lessonContent,
        int creatorId,
        int count = 3,
        string preferredType = "Both")
    {
        if (count < 1) count = 3;
        if (count > 10) count = 10;

        var questions = new List<LessonQuestion>();
        var cleanTitle = string.IsNullOrWhiteSpace(lessonTitle) ? "Bài học" : lessonTitle.Trim();
        var rawContent = lessonContent?.Trim() ?? "";

        // Trích xuất các ý chính, định nghĩa, và gạch đầu dòng
        var extractedFacts = ExtractFactsAndDefinitions(rawContent);
        var bulletPoints   = ExtractBulletPoints(rawContent);
        var sentences      = ExtractMeaningfulSentences(rawContent);

        int currentOrder = 1;
        var requestedType = preferredType?.Trim().ToLowerInvariant() ?? "both";

        // Vòng lặp sinh câu hỏi cho đến khi đủ số lượng
        int factIndex = 0;
        int bulletIndex = 0;
        int sentenceIndex = 0;

        while (questions.Count < count)
        {
            // Xác định loại câu hỏi cho lượt này
            QuestionType targetType;
            if (requestedType == "essay")
            {
                targetType = QuestionType.Essay;
            }
            else if (requestedType == "multiplechoice")
            {
                targetType = QuestionType.MultipleChoice;
            }
            else
            {
                // Xen kẽ: câu chẵn/lẻ
                targetType = (questions.Count % 2 == 0) ? QuestionType.MultipleChoice : QuestionType.Essay;
            }

            LessonQuestion? q = null;

            if (targetType == QuestionType.MultipleChoice)
            {
                // Ưu tiên 1: Sinh từ định nghĩa đã trích xuất
                if (factIndex < extractedFacts.Count)
                {
                    var fact = extractedFacts[factIndex++];
                    q = CreateMultipleChoiceFromFact(lessonId, creatorId, currentOrder, fact, extractedFacts);
                }
                // Ưu tiên 2: Sinh từ danh sách gạch đầu dòng
                else if (bulletIndex < bulletPoints.Count)
                {
                    var bullet = bulletPoints[bulletIndex++];
                    q = CreateMultipleChoiceFromBullet(lessonId, creatorId, currentOrder, cleanTitle, bullet, bulletPoints);
                }
                // Ưu tiên 3: Sinh từ câu có ý nghĩa trong bài
                else if (sentenceIndex < sentences.Count)
                {
                    var sentence = sentences[sentenceIndex++];
                    q = CreateMultipleChoiceFromSentence(lessonId, creatorId, currentOrder, cleanTitle, sentence);
                }
                // Fallback: Sinh câu trắc nghiệm tổng quan theo tiêu đề bài học
                else
                {
                    q = CreateFallbackMultipleChoice(lessonId, creatorId, currentOrder, cleanTitle, questions.Count + 1);
                }
            }
            else
            {
                // Câu hỏi tự luận
                if (factIndex < extractedFacts.Count)
                {
                    var fact = extractedFacts[factIndex++];
                    q = CreateEssayFromFact(lessonId, creatorId, currentOrder, cleanTitle, fact);
                }
                else if (bulletIndex < bulletPoints.Count)
                {
                    var bullet = bulletPoints[bulletIndex++];
                    q = CreateEssayFromBullet(lessonId, creatorId, currentOrder, cleanTitle, bullet);
                }
                else if (sentenceIndex < sentences.Count)
                {
                    var sentence = sentences[sentenceIndex++];
                    q = CreateEssayFromSentence(lessonId, creatorId, currentOrder, cleanTitle, sentence);
                }
                else
                {
                    q = CreateFallbackEssay(lessonId, creatorId, currentOrder, cleanTitle, questions.Count + 1);
                }
            }

            if (q != null)
            {
                questions.Add(q);
                currentOrder++;
            }
        }

        return questions;
    }

    // ── Trích xuất nội dung thông minh ──────────────────────────────────────────

    private record ExtractedFact(string Term, string Definition);

    private static List<ExtractedFact> ExtractFactsAndDefinitions(string content)
    {
        var facts = new List<ExtractedFact>();
        if (string.IsNullOrWhiteSpace(content)) return facts;

        // Pattern 1: "Thuật ngữ là ..." hoặc "Thuật ngữ: ..."
        var patternIs = new Regex(@"(?<term>[\p{L}0-9_\-\s]{2,40})\s+(?:là|được định nghĩa là|được dùng để|có nhiệm vụ|có chức năng)\s+(?<def>[^\.\n;]{10,200})[\.\n;]?", RegexOptions.IgnoreCase);
        foreach (Match match in patternIs.Matches(content))
        {
            var term = match.Groups["term"].Value.Trim(' ', '-', '*', '•', ':');
            var def  = match.Groups["def"].Value.Trim();
            if (term.Length > 2 && def.Length > 8 && !term.Contains('\n') && !facts.Any(f => f.Term.Equals(term, StringComparison.OrdinalIgnoreCase)))
            {
                facts.Add(new ExtractedFact(term, def));
            }
        }

        // Pattern 2: "Khái niệm/Định nghĩa: ..."
        var patternDef = new Regex(@"(?:Khái niệm|Định nghĩa|Ý nghĩa)\s*(?<term>[\p{L}0-9_\-\s]{0,30})\s*:\s*(?<def>[^\.\n;]{10,200})[\.\n;]?", RegexOptions.IgnoreCase);
        foreach (Match match in patternDef.Matches(content))
        {
            var term = match.Groups["term"].Value.Trim();
            var def  = match.Groups["def"].Value.Trim();
            if (string.IsNullOrEmpty(term)) term = "Nội dung bài học";
            if (def.Length > 8 && !facts.Any(f => f.Definition.Equals(def, StringComparison.OrdinalIgnoreCase)))
            {
                facts.Add(new ExtractedFact(term, def));
            }
        }

        return facts;
    }

    private static List<string> ExtractBulletPoints(string content)
    {
        var bullets = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return bullets;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith('-') || line.StartsWith('*') || line.StartsWith('•') || Regex.IsMatch(line, @"^\d+[\.\)]\s+"))
            {
                var clean = Regex.Replace(line, @"^[-*•\d\.\)]+\s*", "").Trim();
                if (clean.Length >= 8)
                {
                    bullets.Add(clean);
                }
            }
        }
        return bullets;
    }

    private static List<string> ExtractMeaningfulSentences(string content)
    {
        var sentences = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return sentences;

        var raw = Regex.Split(content, @"(?<=[.!?\n])\s+");
        foreach (var s in raw)
        {
            var trimmed = s.Trim(' ', '\r', '\n', '-', '*', '•');
            if (trimmed.Length >= 25 && trimmed.Length <= 250 && !trimmed.Contains('\n'))
            {
                sentences.Add(trimmed);
            }
        }
        return sentences;
    }

    // ── Sinh câu trắc nghiệm (Multiple Choice) ──────────────────────────────────

    private static LessonQuestion CreateMultipleChoiceFromFact(
        int lessonId, int creatorId, int order, ExtractedFact fact, List<ExtractedFact> allFacts)
    {
        var questionText = $"Theo nội dung bài học, '{fact.Term}' được hiểu hoặc có vai trò nào sau đây?";
        var correctOption = fact.Definition.Length > 150 ? fact.Definition[..147] + "..." : fact.Definition;

        // Sinh 3 đáp án nhiễu
        var distractors = new List<string>();

        // Lấy từ định nghĩa khác nếu có
        var otherDefs = allFacts.Where(f => !f.Term.Equals(fact.Term, StringComparison.OrdinalIgnoreCase))
                                .Select(f => f.Definition)
                                .ToList();
        foreach (var o in otherDefs)
        {
            if (distractors.Count < 3) distractors.Add(o.Length > 150 ? o[..147] + "..." : o);
        }

        // Bổ sung các phương án phản đề logic nếu chưa đủ 3
        var fallbackDistractors = new[]
        {
            $"Không có vai trò gì liên quan đến {fact.Term}.",
            $"Chỉ áp dụng khi khởi chạy hệ điều hành cục bộ.",
            $"Một thành phần tùy chọn và không được khuyến nghị sử dụng.",
            $"Chỉ được thực thi ở môi trường kiểm thử (Testing) thay vì thực tế."
        };

        foreach (var fd in fallbackDistractors)
        {
            if (distractors.Count < 3 && !distractors.Contains(fd))
                distractors.Add(fd);
        }

        return BuildMultipleChoiceQuestion(lessonId, creatorId, order, questionText, correctOption, distractors);
    }

    private static LessonQuestion CreateMultipleChoiceFromBullet(
        int lessonId, int creatorId, int order, string title, string bullet, List<string> allBullets)
    {
        var questionText = $"Ý nào sau đây là một trong những nội dung / đặc điểm chính được nêu trong bài học '{title}'?";
        var correctOption = bullet.Length > 150 ? bullet[..147] + "..." : bullet;

        var distractors = new List<string>
        {
            "Nội dung này không nằm trong phạm vi kiến thức của bài học.",
            "Phương pháp này đã bị loại bỏ hoàn toàn trong các phiên bản mới.",
            "Chỉ được sử dụng trong các hệ thống phần cứng chuyên biệt."
        };

        return BuildMultipleChoiceQuestion(lessonId, creatorId, order, questionText, correctOption, distractors);
    }

    private static LessonQuestion CreateMultipleChoiceFromSentence(
        int lessonId, int creatorId, int order, string title, string sentence)
    {
        var questionText = $"Khẳng định nào sau đây là ĐÚNG theo nội dung bài học '{title}'?";
        var correctOption = sentence.Length > 150 ? sentence[..147] + "..." : sentence;

        var distractors = new List<string>
        {
            "Khẳng định ngược lại với nguyên lý hoạt động của bài học.",
            "Nội dung này không chính xác theo chuẩn kỹ thuật hiện đại.",
            "Chỉ đúng trong trường hợp hệ thống gặp lỗi ngoại lệ (exception)."
        };

        return BuildMultipleChoiceQuestion(lessonId, creatorId, order, questionText, correctOption, distractors);
    }

    private static LessonQuestion CreateFallbackMultipleChoice(
        int lessonId, int creatorId, int order, string title, int index)
    {
        var questionText = $"Mục tiêu và kiến thức trọng tâm của bài học '{title}' là gì?";
        var correctOption = $"Nắm vững bản chất, nguyên lý và cách ứng dụng của {title} vào thực tế.";
        var distractors = new List<string>
        {
            "Chỉ nhằm mục đích tham khảo lý thuyết mà không có tính ứng dụng.",
            "Thay thế hoàn toàn toàn bộ kiến thức nền tảng đã học.",
            "Một phần kiến thức không bắt buộc và không liên quan đến bài giảng."
        };

        return BuildMultipleChoiceQuestion(lessonId, creatorId, order, questionText, correctOption, distractors);
    }

    private static LessonQuestion BuildMultipleChoiceQuestion(
        int lessonId, int creatorId, int order, string questionText, string correctOption, List<string> distractors)
    {
        var optionsList = new List<(string text, bool isCorrect)>
        {
            (correctOption, true)
        };

        foreach (var d in distractors.Take(3))
        {
            optionsList.Add((d, false));
        }

        // Shuffle thứ tự các phương án để đáp án đúng không luôn nằm ở vị trí đầu
        var shuffled = optionsList.OrderBy(_ => Rnd.Next()).ToList();

        var q = new LessonQuestion
        {
            LessonId     = lessonId,
            CreatorId    = creatorId,
            QuestionText = questionText,
            Type         = QuestionType.MultipleChoice,
            Order        = order,
            CreatedAt    = DateTime.UtcNow,
            Options      = shuffled.Select((opt, idx) => new LessonQuestionOption
            {
                Text      = opt.text,
                IsCorrect = opt.isCorrect,
                Order     = idx + 1
            }).ToList()
        };

        return q;
    }

    // ── Sinh câu tự luận (Essay) ────────────────────────────────────────────────

    private static LessonQuestion CreateEssayFromFact(
        int lessonId, int creatorId, int order, string title, ExtractedFact fact)
    {
        var templates = new[]
        {
            $"Dựa vào bài học '{title}', hãy phân tích và trình bày chi tiết về khái niệm '{fact.Term}'. Nêu ít nhất 1 ví dụ cụ thể minh họa cho khái niệm này.",
            $"Hãy giải thích ý nghĩa và vai trò của '{fact.Term}' trong bài học '{title}'. Vì sao người học cần nắm vững nội dung này?",
            $"Theo nội dung bài học, '{fact.Term}' hoạt động như thế nào? Hãy so sánh ưu điểm của nó so với cách làm truyền thống."
        };

        var questionText = templates[Rnd.Next(templates.Length)];

        return new LessonQuestion
        {
            LessonId     = lessonId,
            CreatorId    = creatorId,
            QuestionText = questionText,
            Type         = QuestionType.Essay,
            Order        = order,
            CreatedAt    = DateTime.UtcNow,
            Options      = []
        };
    }

    private static LessonQuestion CreateEssayFromBullet(
        int lessonId, int creatorId, int order, string title, string bullet)
    {
        var questionText = $"Bài học '{title}' có đề cập đến nội dung: \"{bullet}\". Hãy trình bày chi tiết các bước hoặc nguyên lý để thực hiện tốt nội dung này.";

        return new LessonQuestion
        {
            LessonId     = lessonId,
            CreatorId    = creatorId,
            QuestionText = questionText,
            Type         = QuestionType.Essay,
            Order        = order,
            CreatedAt    = DateTime.UtcNow,
            Options      = []
        };
    }

    private static LessonQuestion CreateEssayFromSentence(
        int lessonId, int creatorId, int order, string title, string sentence)
    {
        var questionText = $"Từ nội dung: \"{sentence}\" trong bài học '{title}', hãy phân tích ý nghĩa và cách áp dụng nội dung này vào giải quyết bài toán thực tế.";

        return new LessonQuestion
        {
            LessonId     = lessonId,
            CreatorId    = creatorId,
            QuestionText = questionText,
            Type         = QuestionType.Essay,
            Order        = order,
            CreatedAt    = DateTime.UtcNow,
            Options      = []
        };
    }

    private static LessonQuestion CreateFallbackEssay(
        int lessonId, int creatorId, int order, string title, int index)
    {
        var questionText = $"Hãy tóm tắt lại các kiến thức trọng tâm trong bài học '{title}' và nêu một bài học kinh nghiệm hoặc ứng dụng thực tế mà bạn rút ra được.";

        return new LessonQuestion
        {
            LessonId     = lessonId,
            CreatorId    = creatorId,
            QuestionText = questionText,
            Type         = QuestionType.Essay,
            Order        = order,
            CreatedAt    = DateTime.UtcNow,
            Options      = []
        };
    }
}
