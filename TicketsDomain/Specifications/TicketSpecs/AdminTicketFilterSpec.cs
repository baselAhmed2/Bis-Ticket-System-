using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;
using TicketsShared.Enums;

namespace TicketsDomain.Specifications.TicketSpecs
{
    public class AdminTicketFilterSpec : BaseSpecification<Ticket, string>
    {
        public AdminTicketFilterSpec(
            int? level = null,
            int? term = null,
            TicketStatus? status = null,
            string? doctorId = null,
            string? subjectId = null,
            string? searchTicketId = null,
            bool? isHighPriority = null,
            int? pageIndex = null,
            int? pageSize = null)
            : base(BuildCriteria(level, term, status, doctorId, subjectId, searchTicketId, isHighPriority))
        {
            AddInclude(t => t.Student);
            AddInclude(t => t.Doctor);
            AddInclude(t => t.Subject);
            AddInclude(t => t.Messages);

            ApplyOrderByDescending(t => t.CreatedAt);

            if (pageIndex.HasValue && pageSize.HasValue)
            {
                ApplyPaging(pageSize.Value, pageIndex.Value);
            }
        }

        private static Expression<Func<Ticket, bool>>? BuildCriteria(
            int? level,
            int? term,
            TicketStatus? status,
            string? doctorId,
            string? subjectId,
            string? searchTicketId,
            bool? isHighPriority)
        {
            // Start with no filter
            Expression<Func<Ticket, bool>>? criteria = null;

            if (!string.IsNullOrWhiteSpace(searchTicketId))
            {
                criteria = t => t.Id.Contains(searchTicketId);
            }
            else
            {
                // Build combined criteria
                criteria = t =>
                    (!level.HasValue || t.Level == level.Value) &&
                    (!term.HasValue || t.Term == term.Value) &&
                    (!status.HasValue || t.Status == status.Value) &&
                    (string.IsNullOrEmpty(doctorId) || t.DoctorId == doctorId) &&
                    (string.IsNullOrEmpty(subjectId) || t.SubjectId == subjectId) &&
                    (!isHighPriority.HasValue || t.IsHighPriority == isHighPriority.Value);
            }

            return criteria;
        }
    }
}
