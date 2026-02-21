using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Doctor
{
    public class DoctorSubjectDto
    {
        public string SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int Level { get; set; }
        public int Term { get; set; }
        public string Program { get; set; } // ✅ مشكلة: ناقصة
        public int TotalTickets { get; set; }
    }
}
