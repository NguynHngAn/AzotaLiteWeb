using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzotaLite.Web.Models
{
    public class Submission
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }
        [ForeignKey(nameof(ExamId))]
        public Exam Exam { get; set; } = default!;

        // StudentId là AspNetUsers.Id (ApplicationUser)
        [Required]
        public string StudentId { get; set; } = default!;
        [ForeignKey(nameof(StudentId))]
        public ApplicationUser Student { get; set; } = default!;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        // Tạm để nullable, sẽ chấm ở Bước 10 (Hangfire)
        public double? Score { get; set; }

        public ICollection<SubmissionAnswer> Answers { get; set; } = new List<SubmissionAnswer>();
    }

    public class SubmissionAnswer
    {
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }
        [ForeignKey(nameof(SubmissionId))]
        public Submission Submission { get; set; } = default!;

        [Required]
        public int QuestionId { get; set; }
        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = default!;

        // Với MCQ: ChoiceId != null; Với tự luận: TextAnswer != null
        public int? ChoiceId { get; set; }
        [ForeignKey(nameof(ChoiceId))]
        public Choice? Choice { get; set; }

        [StringLength(2000)]
        public string? TextAnswer { get; set; }
    }
}
