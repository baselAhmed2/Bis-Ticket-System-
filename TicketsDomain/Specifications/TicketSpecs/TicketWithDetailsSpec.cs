using TicketsDomain.Models;

namespace TicketsDomain.Specifications.TicketSpecs
{
    public class TicketWithDetailsSpec : BaseSpecification<Ticket, string>
    {
        // Get All with Details
        public TicketWithDetailsSpec() : base()
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
        }

        // Get All with Details + Paging
        public TicketWithDetailsSpec(int pageIndex, int pageSize) : base()
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
            ApplyPaging(pageSize, pageIndex);
        }

        // Get By Id with Details
        public TicketWithDetailsSpec(string id)
            : base(t => t.Id == id)
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
        }
    }
}