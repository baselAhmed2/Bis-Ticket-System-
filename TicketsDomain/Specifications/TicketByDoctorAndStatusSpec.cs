using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;
using TicketsShared.Enums;

namespace TicketsDomain.Specifications
{
    public class TicketByDoctorAndStatusSpec : BaseSpecification<Ticket, string>
    {
        public TicketByDoctorAndStatusSpec(string doctorId, TicketStatus status)
            : base(t => t.DoctorId == doctorId && t.Status == status)
        {
        }
    }
}
