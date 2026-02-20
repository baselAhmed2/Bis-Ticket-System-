using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Tickets;
using TicketsShared.Enums;

namespace TiketApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        // =====================
        // Lookup Endpoints (for Create Ticket form)
        // =====================

        // Step 1: الطالب يختار Level + Term → يطلعله المواد
        [HttpGet("subjects")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetSubjects([FromQuery] int level, [FromQuery] int term)
        {
            var subjects = await _ticketService.GetSubjectsByLevelAndTermAsync(level, term);
            return Ok(subjects);
        }

        // Step 2: الطالب يختار الماده → يطلعله الدكاترة
        [HttpGet("doctors-by-subject/{subjectId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetDoctors(string subjectId)
        {
            var doctors = await _ticketService.GetDoctorsBySubjectAsync(subjectId);
            return Ok(doctors);
        }

        // =====================
        // Get Operations
        // =====================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            return Ok(ticket);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var tickets = await _ticketService.GetAllPagedAsync(pageIndex, pageSize);
            return Ok(tickets);
        }

        // =====================
        // Student Operations
        // =====================
        [HttpGet("my-tickets")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyTickets([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var tickets = await _ticketService.GetByStudentIdPagedAsync(userId, pageIndex, pageSize);
            return Ok(tickets);
        }

        // Step 3: الطالب يبعت التيكت
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create([FromBody] CreateTicketDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ticket = await _ticketService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
        }

        // =====================
        // Doctor Operations
        // =====================
        [HttpGet("doctor-tickets")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetDoctorTickets([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var tickets = await _ticketService.GetByDoctorIdPagedAsync(userId, pageIndex, pageSize);
            return Ok(tickets);
        }

        [HttpPost("reply")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Reply([FromBody] ReplyToTicketDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var result = await _ticketService.ReplyAsync(dto, userId, userRole);

            if (!result)
                return BadRequest(new { message = "Failed to send reply" });

            return Ok(new { message = "Reply sent successfully" });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] int status)
        {
            var ticket = await _ticketService.UpdateStatusAsync(id, (TicketStatus)status);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            return Ok(ticket);
        }

        // =====================
        // Admin Operations
        // =====================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _ticketService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Ticket not found" });

            return Ok(new { message = "Ticket deleted successfully" });
        }
    }
}
