using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Analytics
{
    public class DoctorTicketCountDto
    {
        public string DoctorId { get; set; }
        public string DoctorName { get; set; }
        public int TicketCount { get; set; }
        public int NewCount { get; set; }
        public int InProgressCount { get; set; }
        public int ClosedCount { get; set; }
    }
}
