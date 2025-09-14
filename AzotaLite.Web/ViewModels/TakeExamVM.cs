using AzotaLite.Web.Models;

namespace AzotaLite.Web.ViewModels
{
    public class TakeExamVM
    {
        public int SubmissionId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = default!;
        public int DurationMinutes { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; } // StartedAt + Duration

        public List<QuestionItem> Questions { get; set; } = new();
        public Dictionary<int, AnswerValue> ExistingAnswers { get; set; } = new(); // key: QuestionId
        public bool IsSubmitted { get; set; }

        public class QuestionItem
        {
            public int Id { get; set; }
            public string Text { get; set; } = default!;
            public QuestionType Type { get; set; }
            public int Order { get; set; }
            public List<ChoiceItem> Choices { get; set; } = new();
        }

        public class ChoiceItem
        {
            public int Id { get; set; }
            public string Text { get; set; } = default!;
        }

        public class AnswerValue
        {
            public int? ChoiceId { get; set; }     // với trắc nghiệm
            public string? TextAnswer { get; set; } // với tự luận
        }
    }
}
