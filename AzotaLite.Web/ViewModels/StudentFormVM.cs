using System.ComponentModel.DataAnnotations;

namespace AzotaLite.Web.ViewModels
{
    public class StudentFormVM
    {
        public int? Id { get; set; }

        [Required, StringLength(128)]
        public string FullName { get; set; } = default!;

        [EmailAddress, StringLength(128)]
        public string? Email { get; set; }

        [Required]
        public int ClassroomId { get; set; }
    }
}
