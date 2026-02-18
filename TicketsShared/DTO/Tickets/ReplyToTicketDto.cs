using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Tickets
{
    public class ReplyToTicketDto
    {
        public required string TicketId { get; set; }
        public required string Body { get; set; }
                       
    }
}
