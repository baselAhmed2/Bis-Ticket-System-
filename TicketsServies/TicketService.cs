using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsDomain.Specifications.TicketSpecs;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Common;
using TicketsShared.DTO.Lookups;
using TicketsShared.DTO.Tickets;
using TicketsShared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace TicketsServies
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DbContext _context;
        private readonly IMemoryCache _cache;

        public TicketService(IUnitOfWork unitOfWork, DbContext context, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _cache = cache;
        }

        // =====================
        // Get Operations
        // =====================
        public async Task<TicketDto?> GetByIdAsync(string id)
        {
            var spec = new TicketWithDetailsSpec(id);
            var ticket = await _unitOfWork.GetRepository<Ticket, string>()
                .GetAllAsync(spec);

            var result = ticket.FirstOrDefault();
            if (result == null) return null;

            return MapToDto(result);
        }

        public async Task<IEnumerable<TicketDto>> GetAllAsync()
        {
            var spec = new TicketWithDetailsSpec();
            var tickets = await _unitOfWork.GetRepository<Ticket, string>()
                .GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<PagedResultDto<TicketDto>> GetAllPagedAsync(int pageIndex, int pageSize)
        {
            var spec = new TicketWithDetailsSpec(pageIndex, pageSize);
            var countSpec = new TicketWithDetailsSpec();

            var repo = _unitOfWork.GetRepository<Ticket, string>();

            var tickets = await repo.GetAllAsync(spec);
            var totalCount = await repo.CountAsync(countSpec);

            return new PagedResultDto<TicketDto>
            {
                Data = tickets.Select(MapToDto),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        // =====================
        // Lookup Operations
        // =====================
        public async Task<IEnumerable<SubjectLookupDto>> GetSubjectsByLevelAndTermAsync(int level, int term)
        {
            return await _context.Set<Subject>()
                .Where(s => s.Level == level && s.Term == term)
                .Select(s => new SubjectLookupDto
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<DoctorLookupDto>> GetDoctorsBySubjectAsync(string subjectId)
        {
            return await _context.Set<DoctorSubject>()
                .Where(ds => ds.SubjectId == subjectId)
                .Select(ds => new DoctorLookupDto
                {
                    Id = ds.DoctorId,
                    Name = ds.Doctor.Name
                })
                .ToListAsync();
        }

        // =====================
        // Student Operations
        // =====================
        public async Task<IEnumerable<TicketDto>> GetByStudentIdAsync(string studentId)
        {
            var spec = new TicketByStudentSpec(studentId);
            var tickets = await _unitOfWork.GetRepository<Ticket, string>()
                .GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<PagedResultDto<TicketDto>> GetByStudentIdPagedAsync(
            string studentId, int pageIndex, int pageSize)
        {
            var spec = new TicketByStudentSpec(studentId, pageIndex, pageSize);
            var countSpec = new TicketByStudentSpec(studentId);

            var repo = _unitOfWork.GetRepository<Ticket, string>();

            var tickets = await repo.GetAllAsync(spec);
            var totalCount = await repo.CountAsync(countSpec);

            return new PagedResultDto<TicketDto>
            {
                Data = tickets.Select(MapToDto),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<TicketDto> CreateAsync(string studentId, CreateTicketDto dto)
        {
            // Generate Ticket ID
            var ticketId = await GenerateTicketIdAsync(studentId, dto.SubjectId, dto.DoctorId);

            var ticket = new Ticket
            {
                Id = ticketId,
                Title = dto.Title,
                Body = dto.Body,
                Level = dto.Level,
                Term = dto.Term,
                GroupNumber = dto.GroupNumber,
                StudentId = studentId,
                DoctorId = dto.DoctorId,
                SubjectId = dto.SubjectId,
                Status = TicketStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Ticket, string>().AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

                return (await GetByIdAsync(ticketId))!;
        }

        // =====================
        // Doctor Operations
        // =====================
        public async Task<IEnumerable<TicketDto>> GetByDoctorIdAsync(string doctorId)
        {
            var spec = new TicketByDoctorSpec(doctorId);
            var tickets = await _unitOfWork.GetRepository<Ticket, string>()
                .GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<PagedResultDto<TicketDto>> GetByDoctorIdPagedAsync(
            string doctorId, int pageIndex, int pageSize)
        {
            var spec = new TicketByDoctorSpec(doctorId, pageIndex, pageSize);
            var countSpec = new TicketByDoctorSpec(doctorId);

            var repo = _unitOfWork.GetRepository<Ticket, string>();

            var tickets = await repo.GetAllAsync(spec);
            var totalCount = await repo.CountAsync(countSpec);

            return new PagedResultDto<TicketDto>
            {
                Data = tickets.Select(MapToDto),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<bool> ReplyAsync(ReplyToTicketDto dto, string senderId, string senderRole)
        {
            var message = new Message
            {
                TicketId = dto.TicketId,
                SenderId = senderId,
                Body = dto.Body,
                IsHighPriority = senderRole == "Admin",
                SentAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Message, int>().AddAsync(message);
            var result = await _unitOfWork.SaveChangesAsync() > 0;

            if (result)
            {
                // Invalidate doctor stats cache
                var ticket = await _unitOfWork.GetRepository<Ticket, string>()
                    .GetByIdAsync(dto.TicketId);

                if (ticket != null)
                {
                    _cache.Remove($"doctor_stats_{ticket.DoctorId}");
                    _cache.Remove($"doctor_subjects_detail_{ticket.DoctorId}");
                }
            }

            return result;
        }

        public async Task<TicketDto?> UpdateStatusAsync(string id, TicketStatus status)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket, string>()
                .GetByIdAsync(id);

            if (ticket == null) return null;

            ticket.Status = status;
            await _unitOfWork.GetRepository<Ticket, string>().UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate doctor stats cache (status changed)
            _cache.Remove($"doctor_stats_{ticket.DoctorId}");

            return await GetByIdAsync(id);
        }

        // =====================
        // Admin Operations
        // =====================
        public async Task<bool> DeleteAsync(string id)
        {
            // Get ticket before delete to know doctorId for cache invalidation
            var ticket = await _unitOfWork.GetRepository<Ticket, string>()
                .GetByIdAsync(id);

            await _unitOfWork.GetRepository<Ticket, string>().DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync() > 0;

            if (result && ticket != null)
            {
                _cache.Remove($"doctor_stats_{ticket.DoctorId}");
                _cache.Remove($"doctor_subjects_detail_{ticket.DoctorId}");
            }

            return result;
        }

        // =====================
        // Helper Methods
        // =====================
        private async Task<string> GenerateTicketIdAsync(
            string studentId, string subjectId, string doctorId)
        {
            // آخر 4 أرقام من الـ Student ID
            var studentSuffix = studentId.Length >= 4
                ? studentId.Substring(studentId.Length - 4)
                : studentId;

            // عدد التكتات للطالب في نفس المادة
            var countSpec = new TicketByStudentAndSubjectSpec(studentId, subjectId);
            var ticketCount = await _unitOfWork.GetRepository<Ticket, string>()
                .CountAsync(countSpec);

            var ticketNumber = ticketCount + 1;

            return $"{subjectId}-{doctorId}-{studentSuffix}-{ticketNumber}";
        }

        private static TicketDto MapToDto(Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Body = ticket.Body,
                Level = ticket.Level,
                Term = ticket.Term,
                GroupNumber = ticket.GroupNumber,
                Status = ticket.Status,
                IsHighPriority = ticket.IsHighPriority,
                CreatedAt = ticket.CreatedAt,

                StudentId = ticket.StudentId,
                StudentName = ticket.Student?.Name ?? "",

                DoctorId = ticket.DoctorId,
                DoctorName = ticket.Doctor?.Name ?? "",

                SubjectId = ticket.SubjectId,
                SubjectName = ticket.Subject?.Name ?? "",

                Messages = ticket.Messages?.Select(m => new MessageDto
                {
                    Id = m.Id,
                    Body = m.Body,
                    SentAt = m.SentAt,
                    IsHighPriority = m.IsHighPriority,
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.Name ?? ""
                }).ToList() ?? []
            };
        }
    }
}
