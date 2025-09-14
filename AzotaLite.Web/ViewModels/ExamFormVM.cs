using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AzotaLite.Web.ViewModels
{
    public class ExamFormVM
    {
        public int? Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [StringLength(100)]
        public string? Subject { get; set; }

        [Range(1, 600)]
        public int DurationMinutes { get; set; } = 60;

        // chỉ bắt buộc khi Create
        public IFormFile? ExamFile { get; set; }
    }
}
