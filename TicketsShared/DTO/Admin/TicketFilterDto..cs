using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
