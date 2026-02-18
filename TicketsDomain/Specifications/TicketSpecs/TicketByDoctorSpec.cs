using TicketsDomain.Models;

namespace TicketsDomain.Specifications.TicketSpecs
{

    public class TicketByDoctorSpec : BaseSpecification<Ticket, string>
    {
        // Get All Doctor Tickets (???? Pagination)
        public TicketByDoctorSpec(string doctorId)
            : base(t => t.DoctorId == doctorId)
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
        }

        // Get Doctor Tickets with Pagination ? ??? ??
        public TicketByDoctorSpec(string doctorId, int pageIndex, int pageSize)
            : base(t => t.DoctorId == doctorId)
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
            ApplyPaging(pageSize, pageIndex);
        }
    }
}