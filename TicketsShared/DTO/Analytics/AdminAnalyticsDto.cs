using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Analytics
{
    public class AdminAnalyticsDto
    {
        public TicketDistributionDto TicketsByLevel { get; set; }
        public TicketDistributionDto TicketsByStatus { get; set; }
        public List<DoctorTicketCountDto> TopDoctorsByTickets { get; set; }
        public List<SubjectTicketCountDto> TopSubjectsByTickets { get; set; }
        public int TotalTickets { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalStudents { get; set; }
    }
}
