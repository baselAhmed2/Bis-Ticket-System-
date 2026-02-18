using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Tickets
{
    public class MessageDto
    {
        public int Id { get; set; }
        public required string Body { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsHighPriority { get; set; }

        public required string SenderId { get; set; }
        public required string SenderName { get; set; }
    }
}
