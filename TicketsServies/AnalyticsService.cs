using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using TicketsDomain.Models;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Analytics;
using TicketsShared.Enums;

namespace TicketsServies
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly DbContext _context;
        private readonly IMemoryCache _cache;

        public AnalyticsService(DbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<AdminAnalyticsDto> GetAdminAnalyticsAsync()
        {
            const string cacheKey = "admin_analytics";

            if (_cache.TryGetValue(cacheKey, out AdminAnalyticsDto? cachedAnalytics))
                return cachedAnalytics!;

            // ✅ All aggregations in SQL — no loading all tickets to memory

            // Tickets by Level
            var ticketsByLevel = await _context.Set<Ticket>()
                .GroupBy(t => t.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => $"Level {x.Level}", x => x.Count);

            // Tickets by Status
            var ticketsByStatus = await _context.Set<Ticket>()
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Total Tickets
            var totalTickets = await _context.Set<Ticket>().CountAsync();

            // Top Doctors — single query with Join
            var topDoctors = await GetDoctorsByTicketCountAsync(10);

            // Top Subjects — single query with Join
            var topSubjects = await GetSubjectsByTicketCountAsync(10);

            // User Counts — batch role loading
            var roleCounts = await _context.Set<IdentityUserRole<string>>()
                .Join(_context.Set<IdentityRole>(),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .GroupBy(roleName => roleName)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalUsers = await _context.Set<ApplicationUser>().CountAsync();

            var analytics = new AdminAnalyticsDto
            {
                TicketsByLevel = new TicketDistributionDto
                {
                    Distribution = ticketsByLevel
                },
                TicketsByStatus = new TicketDistributionDto
                {
                    Distribution = ticketsByStatus
                },
                TopDoctorsByTickets = topDoctors,
                TopSubjectsByTickets = topSubjects,
                TotalTickets = totalTickets,
                TotalUsers = totalUsers,
                TotalDoctors = roleCounts
                    .FirstOrDefault(x => x.Role == "Doctor")?.Count ?? 0,
                TotalStudents = roleCounts
                    .FirstOrDefault(x => x.Role == "Student")?.Count ?? 0
            };

            // Cache for 10 minutes
            _cache.Set(cacheKey, analytics, TimeSpan.FromMinutes(10));

            return analytics;
        }

        public async Task<List<DoctorTicketCountDto>> GetDoctorsByTicketCountAsync(int topCount = 10)
        {
            // ✅ Single SQL query with Join — no N+1
            return await _context.Set<Ticket>()
                .GroupBy(t => new { t.DoctorId, t.Doctor.Name })
                .Select(g => new DoctorTicketCountDto
                {
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.Name,
                    TicketCount = g.Count(),
                    NewCount = g.Count(t => t.Status == TicketStatus.New),
                    InProgressCount = g.Count(t => t.Status == TicketStatus.InProgress),
                    ClosedCount = g.Count(t => t.Status == TicketStatus.Closed)
                })
                .OrderByDescending(x => x.TicketCount)
                .Take(topCount)
                .ToListAsync();
        }

        public async Task<List<SubjectTicketCountDto>> GetSubjectsByTicketCountAsync(int topCount = 10)
        {
            // ✅ Single SQL query with Join — no N+1
            return await _context.Set<Ticket>()
                .GroupBy(t => new { t.SubjectId, t.Subject.Name, t.Subject.Level })
                .Select(g => new SubjectTicketCountDto
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.Key.Name,
                    Level = g.Key.Level,
                    TicketCount = g.Count()
                })
                .OrderByDescending(x => x.TicketCount)
                .Take(topCount)
                .ToListAsync();
        }
    }
}
