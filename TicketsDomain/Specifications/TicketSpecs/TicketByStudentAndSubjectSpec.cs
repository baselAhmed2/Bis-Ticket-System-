using TicketsDomain.Models;

namespace TicketsDomain.Specifications.TicketSpecs
{
    public class TicketByStudentAndSubjectSpec : BaseSpecification<Ticket, string>
    {
        public TicketByStudentAndSubjectSpec(string studentId, string subjectId)
            : base(t => t.StudentId == studentId && t.SubjectId == subjectId)
        {
        }
    }
}