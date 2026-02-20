using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Analytics
{
    public class SubjectTicketCountDto
    {
        public string SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int Level { get; set; }
        public int TicketCount { get; set; }
    }
}
