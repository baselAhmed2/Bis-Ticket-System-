using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;

namespace TicketsDomain.Specifications
{
    public class TicketByDoctorAndSubjectSpec : BaseSpecification<Ticket, string>
    {
        public TicketByDoctorAndSubjectSpec(string doctorId, string subjectId)
            : base(t => t.DoctorId == doctorId && t.SubjectId == subjectId)
        {
        }
    }
}
