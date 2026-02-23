using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsDomain.Specifications.TicketSpecs;
using TicketsDomain.Specifications.UserSpecs;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Admin;
using TicketsShared.DTO.Common;
using TicketsShared.DTO.Tickets;

namespace TicketsServies
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            DbContext context,
            IUnitOfWork unitOfWork,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _context = context;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        // =====================
        // User Management
        // =====================
        public async Task<PagedResultDto<UserDto>> GetAllUsersAsync(
            int pageIndex, int pageSize, string? searchTerm = null, string? program = null)
        {
            var query = _userManager.Users.AsQueryable();

            // Filter by Program (SubAdmin يشوف بس البرنامج بتاعه)
            if (!string.IsNullOrEmpty(program))
            {
                query = query.Where(u => u.Program == program);
            }

            // Apply Search
            query = UserSearchSpec.ApplySearch(query, searchTerm);

            var totalCount = await query.CountAsync();

            // Apply Paging
            query = UserSearchSpec.ApplyPaging(query, pageIndex, pageSize);

            var users = await query.ToListAsync();

            // Batch load roles
            var userIds = users.Select(u => u.Id).ToList();
            var userRoles = await _context.Set<IdentityUserRole<string>>()
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Set<IdentityRole>(),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, RoleName = r.Name })
                .ToListAsync();

            var roleMap = userRoles.ToDictionary(x => x.UserId, x => x.RoleName ?? "Unknown");

            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Id,
                Name = user.Name,
                Role = roleMap.GetValueOrDefault(user.Id, "Unknown"),
                Program = user.Program
            }).ToList();

            return new PagedResultDto<UserDto>
            {
                Data = userDtos,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Id,
                Name = user.Name,
                Role = roles.FirstOrDefault() ?? "Unknown",
                Program = user.Program
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new ApplicationUser
            {
                Id = dto.Id,
                UserName = dto.Id,
                Name = dto.Name,
                Program = dto.Program
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, dto.Role);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Role = dto.Role,
                Program = dto.Program
            };
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        // =====================
        // Doctor Subject Management
        // =====================
        public async Task<bool> AssignSubjectsToDoctorAsync(AssignSubjectsDto dto)
        {
            var existing = await _context.Set<DoctorSubject>()
                .Where(ds => ds.DoctorId == dto.DoctorId)
                .ToListAsync();

            _context.Set<DoctorSubject>().RemoveRange(existing);

            foreach (var subjectId in dto.SubjectIds)
            {
                _context.Set<DoctorSubject>().Add(new DoctorSubject
                {
                    DoctorId = dto.DoctorId,
                    SubjectId = subjectId
                });
            }

            // Invalidate caches
            _cache.Remove("all_subjects");
            _cache.Remove($"doctor_subjects_{dto.DoctorId}");

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<SubjectDto>> GetDoctorSubjectsAsync(string doctorId)
        {
            var cacheKey = $"doctor_subjects_{doctorId}";

            // Try get from cache
            if (_cache.TryGetValue(cacheKey, out IEnumerable<SubjectDto>? cachedSubjects))
                return cachedSubjects!;

            // Get from DB
            var result = await _context.Set<DoctorSubject>()
                .Where(ds => ds.DoctorId == doctorId)
                .Include(ds => ds.Subject)
                .Select(ds => new SubjectDto
                {
                    Id = ds.Subject.Id,
                    Name = ds.Subject.Name,
                    Level = ds.Subject.Level,
                    Term = ds.Subject.Term,
                    Program = ds.Subject.Program
                })
                .ToListAsync();

            // Cache for 30 minutes
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

            return result;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(string? program = null)
        {
            var cacheKey = string.IsNullOrEmpty(program) ? "all_subjects" : $"subjects_{program}";

            // Try get from cache
            if (_cache.TryGetValue(cacheKey, out IEnumerable<SubjectDto>? cachedSubjects))
                return cachedSubjects!;

            // Get from DB
            var query = _context.Set<Subject>().AsQueryable();

            if (!string.IsNullOrEmpty(program))
            {
                query = query.Where(s => s.Program == program);
            }

            var subjectDtos = await query
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Level = s.Level,
                    Term = s.Term,
                    Program = s.Program
                })
                .ToListAsync();

            // Cache for 1 hour
            _cache.Set(cacheKey, subjectDtos, TimeSpan.FromHours(1));

            return subjectDtos;
        }

        // =====================
        // Admin Subject Assignment (NEW)
        // =====================
        public async Task<bool> AssignAdminToSubjectAsync(string adminId, string subjectId)
        {
            // تأكد إن مفيش ربط موجود أصلاً
            var exists = await _context.Set<DoctorSubject>()
                .AnyAsync(ds => ds.DoctorId == adminId && ds.SubjectId == subjectId);

            if (exists) return true; // مربوط فعلاً

            _context.Set<DoctorSubject>().Add(new DoctorSubject
            {
                DoctorId = adminId,
                SubjectId = subjectId
            });

            _cache.Remove($"doctor_subjects_{adminId}");
            _cache.Remove("all_subjects");

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveAdminFromSubjectAsync(string adminId, string subjectId)
        {
            var record = await _context.Set<DoctorSubject>()
                .FirstOrDefaultAsync(ds => ds.DoctorId == adminId && ds.SubjectId == subjectId);

            if (record == null) return false;

            _context.Set<DoctorSubject>().Remove(record);

            _cache.Remove($"doctor_subjects_{adminId}");
            _cache.Remove("all_subjects");

            return await _context.SaveChangesAsync() > 0;
        }

        // =====================
        // Admin Messages (NEW)
        // =====================
        public async Task<PagedResultDto<AdminMessageDto>> GetAdminMessagesAsync(
            string adminId, int pageIndex, int pageSize)
        {
            // الرسائل اللي في التذاكر اللي الأدمن رد عليها
            // (التذاكر اللي فيها رسالة من الأدمن ده)
            var ticketIdsWithAdminMessages = _context.Set<Message>()
                .Where(m => m.SenderId == adminId)
                .Select(m => m.TicketId)
                .Distinct();

            var query = _context.Set<Message>()
                .Where(m => ticketIdsWithAdminMessages.Contains(m.TicketId))
                .Include(m => m.Sender)
                .Include(m => m.Ticket)
                    .ThenInclude(t => t.Student)
                .Include(m => m.Ticket)
                    .ThenInclude(t => t.Doctor)
                .OrderByDescending(m => m.SentAt);

            var totalCount = await query.CountAsync();

            var messages = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new AdminMessageDto
                {
                    MessageId = m.Id,
                    Body = m.Body,
                    SentAt = m.SentAt,
                    SenderName = m.Sender.Name,
                    SenderId = m.SenderId,
                    TicketId = m.TicketId,
                    TicketTitle = m.Ticket.Title,
                    StudentName = m.Ticket.Student.Name,
                    DoctorName = m.Ticket.Doctor.Name
                })
                .ToListAsync();

            return new PagedResultDto<AdminMessageDto>
            {
                Data = messages,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        // =====================
        // Ticket Monitoring
        // =====================
        public async Task<PagedResultDto<TicketDto>> GetAllTicketsFilteredAsync(TicketFilterDto filter)
        {
            var spec = new AdminTicketFilterSpec(
                level: filter.Level,
                term: filter.Term,
                status: filter.Status,
                doctorId: filter.DoctorId,
                subjectId: filter.SubjectId,
                searchTicketId: filter.SearchTicketId,
                isHighPriority: filter.IsHighPriority,
                program: filter.Program,
                pageIndex: filter.PageIndex,
                pageSize: filter.PageSize
            );

            var countSpec = new AdminTicketFilterSpec(
                level: filter.Level,
                term: filter.Term,
                status: filter.Status,
                doctorId: filter.DoctorId,
                subjectId: filter.SubjectId,
                searchTicketId: filter.SearchTicketId,
                isHighPriority: filter.IsHighPriority,
                program: filter.Program
            );

            var ticketRepo = _unitOfWork.GetRepository<Ticket, string>();

            var tickets = await ticketRepo.GetAllAsync(spec);
            var totalCount = await ticketRepo.CountAsync(countSpec);

            return new PagedResultDto<TicketDto>
            {
                Data = tickets.Select(MapToDto),
                TotalCount = totalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize
            };
        }

        public async Task<IEnumerable<TicketDto>> GetHighPriorityTicketsAsync(string? program = null)
        {
            var spec = new HighPriorityTicketsSpec(program);
            var tickets = await _unitOfWork.GetRepository<Ticket, string>()
                .GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<PagedResultDto<TicketDto>> GetHighPriorityTicketsPagedAsync(
            int pageIndex, int pageSize, string? program = null)
        {
            var spec = new HighPriorityTicketsSpec(pageIndex, pageSize, program);
            var countSpec = new HighPriorityTicketsSpec(program);

            var ticketRepo = _unitOfWork.GetRepository<Ticket, string>();

            var tickets = await ticketRepo.GetAllAsync(spec);
            var totalCount = await ticketRepo.CountAsync(countSpec);

            return new PagedResultDto<TicketDto>
            {
                Data = tickets.Select(MapToDto),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<bool> MarkTicketAsHighPriorityAsync(string ticketId, bool isHighPriority)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket, string>()
                .GetByIdAsync(ticketId);

            if (ticket == null) return false;

            ticket.IsHighPriority = isHighPriority;
            await _unitOfWork.GetRepository<Ticket, string>().UpdateAsync(ticket);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        // =====================
        // Helper Method
        // =====================
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
                Program = ticket.Program, // ✅ FIX

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
