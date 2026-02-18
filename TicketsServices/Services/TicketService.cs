    using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsDomain.Specifications.TicketSpecs;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Tickets;
using TicketsShared.Enums;

namespace TicketsServices.Services
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TicketService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TicketDto> CreateAsync(string studentId, CreateTicketDto dto)
        {
            var repo = _unitOfWork.GetRepository<Ticket, string>();

            var ticketId = await GenerateTicketIdAsync(studentId, dto.SubjectId, dto.DoctorId);

            var ticket = new Ticket
            {
                Id = ticketId,
                Title = dto.Title,
                Body = dto.Body,
                GroupNumber = dto.GroupNumber,
                SubjectId = dto.SubjectId,
                DoctorId = dto.DoctorId,
                StudentId = studentId,
                Level = dto.Level,
                Term = dto.Term,
                IsHighPriority = dto.IsHighPriority,
                Status = TicketStatus.New
            };

            await repo.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            var result = await GetByIdAsync(ticket.Id);
            return result!;
        }

        public async Task<TicketDto?> GetByIdAsync(string id)
        {
            var repo = _unitOfWork.GetRepository<Ticket, string>();
            var spec = new TicketWithDetailsSpec(id);
            var tickets = await repo.GetAllAsync(spec);
            var ticket = tickets.FirstOrDefault();

            return ticket is null ? null : MapToDto(ticket);
        }

        public async Task<IEnumerable<TicketDto>> GetAllAsync()
        {
            var repo = _unitOfWork.GetRepository<Ticket, string>();
            var spec = new TicketWithDetailsSpec();
            var tickets = await repo.GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<IEnumerable<TicketDto>> GetByStudentIdAsync(string studentId)
        {
            var repo = _unitOfWork.GetRepository<Ticket, string>();
            var spec = new TicketByStudentSpec(studentId);
            var tickets = await repo.GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<IEnumerable<TicketDto>> GetByDoctorIdAsync(string doctorId)
        {
            var repo = _unitOfWork.GetRepository<Ticket, string>();
            var spec = new TicketByDoctorSpec(doctorId);
            var tickets = await repo.GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<TicketDto?> UpdateStatusAsync(string id, TicketStatus status)
        {
            var repo = _unitOfWork.GetRepository<Ticket, string>();
            var ticket = await repo.GetByIdAsync(id);

            if (ticket is null) return null;

            ticket.Status = status;
            await repo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var repo = _unitOfWork.GetRepository<Ticket, string>();
            var ticket = await repo.GetByIdAsync(id);

            if (ticket is null) return false;

            await repo.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // =====================
        // Generate Ticket ID
        // =====================
        private async Task<string> GenerateTicketIdAsync(
            string studentId, string subjectId, string doctorId)
        {
            var studentSuffix = studentId.Substring(studentId.Length - 4);

            var ticketCount = await _unitOfWork
                .GetRepository<Ticket, string>()
                .CountAsync(new TicketByStudentAndSubjectSpec(studentId, subjectId));

            var ticketNumber = ticketCount + 1;

            return $"{subjectId}-{doctorId}-{studentSuffix}-{ticketNumber}";
        }

        // =====================
        // Manual Mapping
        // =====================
        private static TicketDto MapToDto(Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Body = ticket.Body,
                GroupNumber = ticket.GroupNumber,
                Status = ticket.Status,
                IsHighPriority = ticket.IsHighPriority,
                CreatedAt = ticket.CreatedAt,
                Level = ticket.Level,
                Term = ticket.Term,
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