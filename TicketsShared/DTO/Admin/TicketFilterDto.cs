using TicketsShared.Enums;

namespace TicketsShared.DTO.Admin
{
    public class TicketFilterDto
    {
        public int? Level { get; set; }
        public int? Term { get; set; }
        public TicketStatus? Status { get; set; }
        public string? DoctorId { get; set; }
        public string? SubjectId { get; set; }
        public string? SearchTicketId { get; set; }
        public bool? IsHighPriority { get; set; }
        public string? Program { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}