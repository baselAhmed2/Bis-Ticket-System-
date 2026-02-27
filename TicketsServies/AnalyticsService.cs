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

        public async Task<TicketCountsDto> GetTicketCountsAsync(
            string? program = null,
            int? level = null,
            string? period = null,
            string? subjectId = null,
            string? doctorId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            // Support easy human-friendly periods: "day", "week", "month", "3months", "year"
            var now = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(period) && !from.HasValue && !to.HasValue)
            {
                switch (period.ToLower())
                {
                    case "day":
                    case "24h":
                        from = now.AddDays(-1);
                        break;
                    case "week":
                    case "7d":
                        from = now.AddDays(-7);
                        break;
                    case "month":
                    case "30d":
                        from = now.AddMonths(-1);
                        break;
                    case "3months":
                    case "90d":
                        from = now.AddMonths(-3);
                        break;
                    case "year":
                    case "365d":
                        from = now.AddYears(-1);
                        break;
                    default:
                        // unsupported period — ignore
                        break;
                }
                to = now;
            }

            var query = _context.Set<Ticket>().AsQueryable();

            if (!string.IsNullOrEmpty(program))
                query = query.Where(t => t.Program == program);

            if (level.HasValue)
                query = query.Where(t => t.Level == level.Value);


            if (!string.IsNullOrEmpty(subjectId))
                query = query.Where(t => t.SubjectId == subjectId);

            if (!string.IsNullOrEmpty(doctorId))
                query = query.Where(t => t.DoctorId == doctorId);

            if (from.HasValue)
                query = query.Where(t => t.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(t => t.CreatedAt <= to.Value);

            var total = await query.CountAsync();
            var newCount = await query.CountAsync(t => t.Status == TicketStatus.New);
            var inProgress = await query.CountAsync(t => t.Status == TicketStatus.InProgress);
            var closed = await query.CountAsync(t => t.Status == TicketStatus.Closed);
            var highPriority = await query.CountAsync(t => t.IsHighPriority);

            // Replied tickets: have at least one message
            var replied = await _context.Set<Ticket>()
                .Where(t => query.Select(q => q.Id).Contains(t.Id))
                .Where(t => t.Messages.Any())
                .CountAsync();

            var unreplied = total - replied;

            return new TicketCountsDto
            {
                TotalTickets = total,
                NewTickets = newCount,
                InProgressTickets = inProgress,
                ClosedTickets = closed,
                HighPriorityTickets = highPriority,
                RepliedTickets = replied,
                UnrepliedTickets = unreplied
            };
        }

        public async Task<AdminAnalyticsDto> GetAdminAnalyticsAsync()
        {
            const string cacheKey = "admin_analytics";

            if (_cache.TryGetValue(cacheKey, out AdminAnalyticsDto? cachedAnalytics))
                return cachedAnalytics!;

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

            // Top Doctors — no level filter for general analytics
            var topDoctors = await GetDoctorsByTicketCountAsync(10);

            // Top Subjects — no level filter for general analytics
            var topSubjects = await GetSubjectsByTicketCountAsync(10);

            // User Counts
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

            _cache.Set(cacheKey, analytics, TimeSpan.FromMinutes(10));

            return analytics;
        }

        public async Task<List<DoctorTicketCountDto>> GetDoctorsByTicketCountAsync(
            int topCount = 10, int? level = null)
        {
            var query = _context.Set<Ticket>().AsQueryable();

            if (level.HasValue)
                query = query.Where(t => t.Level == level.Value);

            return await query
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

        public async Task<List<SubjectTicketCountDto>> GetSubjectsByTicketCountAsync(
            int topCount = 10, int? level = null)
        {
            var query = _context.Set<Ticket>().AsQueryable();

            if (level.HasValue)
                query = query.Where(t => t.Level == level.Value);

            return await query
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
