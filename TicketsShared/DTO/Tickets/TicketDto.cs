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
        public string Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int Level { get; set; }
        public int Term { get; set; }
        public int GroupNumber { get; set; }
        public TicketStatus Status { get; set; }
        public bool IsHighPriority { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Program { get; set; } // ✅ مشكلة: ناقصة

        // Student Info
        public string StudentId { get; set; }
        public string StudentName { get; set; }

        // Doctor Info
        public string DoctorId { get; set; }
        public string DoctorName { get; set; }

        // Subject Info
        public string SubjectId { get; set; }
        public string SubjectName { get; set; }

        // Messages
        public List<MessageDto> Messages { get; set; } = [];
    }
}