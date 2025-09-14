using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzotaLite.Web.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [ForeignKey(nameof(ExamId))]
        public Exam Exam { get; set; } = default!;

        [Required, StringLength(1000)]
        public string Text { get; set; } = default!;

        [Required]
        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;

        // Thứ tự hiển thị trong đề
        [Range(1, 10000)]
        public int Order { get; set; }

        public ICollection<Choice> Choices { get; set; } = new List<Choice>();
    }

    public class Choice
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = default!;

        [Required, StringLength(500)]
        public string Text { get; set; } = default!;

        public bool IsCorrect { get; set; }
    }
}
