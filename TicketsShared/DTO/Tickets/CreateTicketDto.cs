using System.ComponentModel.DataAnnotations;

namespace TicketsShared.DTO.Tickets
{
    public class CreateTicketDto
    {
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public required string Title { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public required string Body { get; set; }

        [Range(1, 4)]
        public int Level { get; set; }

        [Range(1, 2)]
        public int Term { get; set; }

        [Range(1, 20)]
        public int GroupNumber { get; set; }

        [Required]
        public required string DoctorId { get; set; }

        [Required]
        public required string SubjectId { get; set; }
    }
}