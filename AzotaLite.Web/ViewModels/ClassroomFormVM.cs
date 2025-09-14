using System.ComponentModel.DataAnnotations;

namespace AzotaLite.Web.ViewModels
{
    public class ClassroomFormVM
    {
        public int? Id { get; set; }

        [Required, StringLength(128)]
        public string Name { get; set; } = default!;

        [StringLength(16)]
        public string? Grade { get; set; }
    }
}
