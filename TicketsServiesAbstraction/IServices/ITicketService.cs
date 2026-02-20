using TicketsShared.DTO.Common;
using TicketsShared.DTO.Lookups;
using TicketsShared.DTO.Tickets;
using TicketsShared.Enums;

namespace TicketsServiesAbstraction.IServices
{
    public interface ITicketService
    {
        // Get Operations
        Task<TicketDto?> GetByIdAsync(string id);
        Task<PagedResultDto<TicketDto>> GetAllPagedAsync(int pageIndex, int pageSize);

        // Lookup Operations
        Task<IEnumerable<SubjectLookupDto>> GetSubjectsByLevelAndTermAsync(int level, int term);
        Task<IEnumerable<DoctorLookupDto>> GetDoctorsBySubjectAsync(string subjectId);

        // Student Operations
        Task<PagedResultDto<TicketDto>> GetByStudentIdPagedAsync(
            string studentId, int pageIndex, int pageSize);
        Task<TicketDto> CreateAsync(string studentId, CreateTicketDto dto);

        // Doctor Operations
        Task<PagedResultDto<TicketDto>> GetByDoctorIdPagedAsync(
            string doctorId, int pageIndex, int pageSize);
        Task<bool> ReplyAsync(ReplyToTicketDto dto, string senderId, string senderRole);
        Task<TicketDto?> UpdateStatusAsync(string id, TicketStatus status);

        // Admin Operations
        Task<bool> DeleteAsync(string id);
    }
}