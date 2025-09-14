using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzotaLite.Web.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [StringLength(100)]
        public string? Subject { get; set; }

        [Range(1, 600)]
        public int DurationMinutes { get; set; } = 60;

        // Đường dẫn nội bộ tới file đã lưu (GUID + ext), KHÔNG phải tên gốc
        [Required, StringLength(400)]
        public string FilePath { get; set; } = default!;

        [Required, StringLength(260)]
        public string OriginalFileName { get; set; } = default!;

        [Required, StringLength(200)]
        public string ContentType { get; set; } = default!;

        public long FileSize { get; set; }

        // FK tới Teacher (Owner)
        [Required]
        public string OwnerTeacherId { get; set; } = default!;

        [ForeignKey(nameof(OwnerTeacherId))]
        public ApplicationUser OwnerTeacher { get; set; } = default!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    }
}
