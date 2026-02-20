using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;

namespace TicketsDomain.Specifications.TicketSpecs
{
    public class HighPriorityTicketsSpec : BaseSpecification<Ticket, string>
    {
        public HighPriorityTicketsSpec()
            : base(t => t.IsHighPriority == true)
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            ApplyOrderByDescending(t => t.CreatedAt);
        }

        public HighPriorityTicketsSpec(int pageIndex, int pageSize)
            : base(t => t.IsHighPriority == true)
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            ApplyOrderByDescending(t => t.CreatedAt);
            ApplyPaging(pageSize, pageIndex);
        }
    }
}
