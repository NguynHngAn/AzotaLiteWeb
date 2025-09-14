using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzotaLite.Web.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required, StringLength(128)]
        public string FullName { get; set; } = default!;

        [EmailAddress, StringLength(128)]
        public string? Email { get; set; }

        // bắt buộc phải thuộc 1 lớp
        [Required]
        public int ClassroomId { get; set; }

        [ForeignKey(nameof(ClassroomId))]
        public Classroom Classroom { get; set; } = default!;

        // Bạn có thể bổ sung Mã HS, Ngày sinh... sau
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
