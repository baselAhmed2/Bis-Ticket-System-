using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TicketsShared.Enums;

namespace TicketsDomain.Models
{
    public class Ticket : BaseEntity<string>
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public int GroupNumber { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.New;
        public bool IsHighPriority { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Program { get; set; } // BIS, FMI, CS

        // Relations
        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }

        public string DoctorId { get; set; }
        public ApplicationUser Doctor { get; set; }

        public string SubjectId { get; set; }
        public Subject Subject { get; set; }

        public int Level { get; set; }
        public int Term { get; set; }

        public ICollection<Message> Messages { get; set; }
    }
}