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
        // Get All (with optional program filter)
        public HighPriorityTicketsSpec(string? program = null)
            : base(t => t.IsHighPriority
                && (program == null || t.Program == program))
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
        }

        // Get All with Paging (with optional program filter)
        public HighPriorityTicketsSpec(int pageIndex, int pageSize, string? program = null)
            : base(t => t.IsHighPriority
                && (program == null || t.Program == program))
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
            ApplyPaging(pageSize, pageIndex);
        }
    }
}
