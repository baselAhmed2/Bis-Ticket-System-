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
        Task<AdminAnalyticsDto> GetAdminAnalyticsAsync(int? periodInDays = null);
        Task<List<DoctorTicketCountDto>> GetDoctorsByTicketCountAsync(int topCount = 10, int? level = null);
        Task<List<SubjectTicketCountDto>> GetSubjectsByTicketCountAsync(int topCount = 10, int? level = null);
    }
}
