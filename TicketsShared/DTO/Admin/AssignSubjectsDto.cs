using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Admin
{
    public class AssignSubjectsDto
    {
        public string DoctorId { get; set; }
        public List<string> SubjectIds { get; set; }
    }
}
