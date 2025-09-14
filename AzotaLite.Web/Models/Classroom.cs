using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AzotaLite.Web.Models; // ApplicationUser

namespace AzotaLite.Web.Models
{
    public class Classroom
    {
        public int Id { get; set; }

        [Required, StringLength(128)]
        public string Name { get; set; } = default!;

        [StringLength(16)]
        public string? Grade { get; set; } // "10", "11", "12" ...

        [Required]
        public string TeacherId { get; set; } = default!; // FK tới ApplicationUser

        [ForeignKey(nameof(TeacherId))]
        public ApplicationUser Teacher { get; set; } = default!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
