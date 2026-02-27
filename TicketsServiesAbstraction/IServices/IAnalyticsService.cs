using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsShared.DTO.Analytics;

namespace TicketsServiesAbstraction.IServices
{
    public interface IAnalyticsService
    {
        Task<AdminAnalyticsDto> GetAdminAnalyticsAsync();
        Task<List<DoctorTicketCountDto>> GetDoctorsByTicketCountAsync(int topCount = 10, int? level = null);
        Task<List<SubjectTicketCountDto>> GetSubjectsByTicketCountAsync(int topCount = 10, int? level = null);
        Task<TicketCountsDto> GetTicketCountsAsync(
            string? program = null,
            int? level = null,
            string? period = null,
            string? subjectId = null,
            string? doctorId = null,
            DateTime? from = null,
            DateTime? to = null
        );
    }
}
