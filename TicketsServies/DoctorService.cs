using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsDomain.Specifications;
using TicketsDomain.Specifications.TicketSpecs;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Common;
using TicketsShared.DTO.Doctor;
using TicketsShared.DTO.Lookups;
using TicketsShared.DTO.Tickets;
using TicketsShared.Enums;

namespace TicketsServies
{
    public class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DbContext _context;
        private readonly IMemoryCache _cache;

        public DoctorService(IUnitOfWork unitOfWork, DbContext context, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _context = context; 
            _cache = cache;
        }

        public async Task<DoctorStatsDto> GetDoctorStatsAsync(string doctorId)
        {
            var cacheKey = $"doctor_stats_{doctorId}";

            if (_cache.TryGetValue(cacheKey, out DoctorStatsDto? cachedStats))
                return cachedStats!;

            // ✅ Single query with GroupBy instead of 4 separate queries
            var statusCounts = await _context.Set<Ticket>()
                .Where(t => t.DoctorId == doctorId)
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var newCount = statusCounts
                .FirstOrDefault(x => x.Status == TicketStatus.New)?.Count ?? 0;
            var inProgressCount = statusCounts
                .FirstOrDefault(x => x.Status == TicketStatus.InProgress)?.Count ?? 0;
            var closedCount = statusCounts
                .FirstOrDefault(x => x.Status == TicketStatus.Closed)?.Count ?? 0;

            var stats = new DoctorStatsDto
            {
                NewTickets = newCount,
                InProgressTickets = inProgressCount,
                ClosedTickets = closedCount,
                TotalTickets = newCount + inProgressCount + closedCount
            };

            // Cache for 5 minutes
            _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));

            return stats;
        }

        public async Task<IEnumerable<DoctorSubjectDto>> GetDoctorSubjectsAsync(string doctorId)
        {
            var cacheKey = $"doctor_subjects_detail_{doctorId}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<DoctorSubjectDto>? cachedSubjects))
                return cachedSubjects!;

            // ✅ Single query instead of N+1
            var doctorSubjects = await _context.Set<DoctorSubject>()
                .Where(ds => ds.DoctorId == doctorId)
                .Include(ds => ds.Subject)
                .Select(ds => new DoctorSubjectDto
                {
                    SubjectId = ds.SubjectId,
                    SubjectName = ds.Subject.Name,
                    Level = ds.Subject.Level,
                    Term = ds.Subject.Term,
                    TotalTickets = _context.Set<Ticket>()
                        .Count(t => t.DoctorId == doctorId && t.SubjectId == ds.SubjectId)
                })
                .ToListAsync();

            // Cache for 10 minutes
            _cache.Set(cacheKey, doctorSubjects, TimeSpan.FromMinutes(10));

            return doctorSubjects;
        }
    }
}
