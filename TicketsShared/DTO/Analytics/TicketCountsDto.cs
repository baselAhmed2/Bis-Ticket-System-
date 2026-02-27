using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Analytics
{
    public class TicketCountsDto
    {
        public int TotalTickets { get; set; }
        public int NewTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int RepliedTickets { get; set; }
        public int UnrepliedTickets { get; set; }
        public int HighPriorityTickets { get; set; }
    }
}
