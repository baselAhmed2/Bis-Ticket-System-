using TicketsDomain.Models;

namespace TicketsDomain.Specifications.TicketSpecs
{
    public class TicketByStudentSpec : BaseSpecification<Ticket, string>
    {
        // Get All Student Tickets (بدون Pagination)
        public TicketByStudentSpec(string studentId)
            : base(t => t.StudentId == studentId)
        {
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
        }

        // Get Student Tickets with Pagination
        public TicketByStudentSpec(string studentId, int pageIndex, int pageSize)
            : base(t => t.StudentId == studentId)
        {
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);
            ApplyOrderByDescending(t => t.CreatedAt);
            ApplyPaging(pageSize, pageIndex);
        }
    }
}