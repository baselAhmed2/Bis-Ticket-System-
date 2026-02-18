using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsShared.Enums;

namespace TicketsShared.DTO.Tickets
{
    public class TicketDto
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Body { get; set; }
        public int Level { get; set; }
        public int Term { get; set; }
        public int GroupNumber { get; set; }
        public TicketStatus Status { get; set; }
        public bool IsHighPriority { get; set; }
        public DateTime CreatedAt { get; set; }

        // Student Info
        public required string StudentId { get; set; }
        public required string StudentName { get; set; }

        // Doctor Info
        public required string DoctorId { get; set; }
        public required string DoctorName { get; set; }

        // Subject Info
        public required string SubjectId { get; set; }
        public required string SubjectName { get; set; }

        // Messages
        public required List<MessageDto> Messages { get; set; }
    }
}