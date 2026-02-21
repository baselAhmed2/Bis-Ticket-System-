using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsShared.DTO.Admin;
using TicketsShared.DTO.Common;
using TicketsShared.DTO.Tickets;

namespace TicketsServiesAbstraction.IServices
{
    public interface IAdminService
    {
        // User Management
        Task<PagedResultDto<UserDto>> GetAllUsersAsync(
            int pageIndex, int pageSize, string? searchTerm = null, string? program = null);
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
        Task<bool> DeleteUserAsync(string userId);

        // Doctor Subject Management
        Task<bool> AssignSubjectsToDoctorAsync(AssignSubjectsDto dto);
        Task<IEnumerable<SubjectDto>> GetDoctorSubjectsAsync(string doctorId);
        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(string? program = null);


        // Ticket Monitoring
        Task<PagedResultDto<TicketDto>> GetAllTicketsFilteredAsync(TicketFilterDto filter);
        Task<IEnumerable<TicketDto>> GetHighPriorityTicketsAsync(string? program = null);
        Task<PagedResultDto<TicketDto>> GetHighPriorityTicketsPagedAsync(
            int pageIndex, int pageSize, string? program = null);
        Task<bool> MarkTicketAsHighPriorityAsync(string ticketId, bool isHighPriority);
    }
}
