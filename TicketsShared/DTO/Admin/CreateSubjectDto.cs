using System.ComponentModel.DataAnnotations;

namespace TicketsShared.DTO.Admin
{
    public class CreateSubjectDto
    {
        [Required]
        [StringLength(50)]
        public required string Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        [Range(1, 4)]
        public int Level { get; set; }

        [Range(1, 2)]
        public int Term { get; set; }

        [Required]
        [StringLength(20)]
        public required string Program { get; set; }
    }
}
