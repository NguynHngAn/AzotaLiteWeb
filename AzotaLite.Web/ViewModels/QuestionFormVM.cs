using System.ComponentModel.DataAnnotations;
using AzotaLite.Web.Models;

namespace AzotaLite.Web.ViewModels
{
    public class QuestionFormVM
    {
        public int? Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required, StringLength(1000)]
        public string Text { get; set; } = default!;

        [Required]
        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;

        [Range(1, 10000)]
        public int Order { get; set; } = 1;

        // Choice nhập nhanh (tuỳ chọn)
        public List<ChoiceItemVM> Choices { get; set; } = new();
    }

    public class ChoiceItemVM
    {
        public int? Id { get; set; }
        [Required, StringLength(500)]
        public string Text { get; set; } = default!;
        public bool IsCorrect { get; set; }
    }
}
