using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Admin;

namespace TiketApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // =====================
        // User Management
        // =====================
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            var users = await _adminService.GetAllUsersAsync(pageIndex, pageSize, searchTerm);
            return Ok(users);
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var user = await _adminService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                var user = await _adminService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await _adminService.DeleteUserAsync(userId);

            if (!result)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "User deleted successfully" });
        }

        // =====================
        // Doctor Subject Management
        // =====================
        [HttpPost("doctors/assign-subjects")]
        public async Task<IActionResult> AssignSubjects([FromBody] AssignSubjectsDto dto)
        {
            var result = await _adminService.AssignSubjectsToDoctorAsync(dto);

            if (!result)
                return BadRequest(new { message = "Failed to assign subjects" });

            return Ok(new { message = "Subjects assigned successfully" });
        }

        [HttpGet("doctors/{doctorId}/subjects")]
        public async Task<IActionResult> GetDoctorSubjects(string doctorId)
        {
            var subjects = await _adminService.GetDoctorSubjectsAsync(doctorId);
            return Ok(subjects);
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetAllSubjects()
        {
            var subjects = await _adminService.GetAllSubjectsAsync();
            return Ok(subjects);
        }

        // =====================
        // Ticket Monitoring
        // =====================
        [HttpPost("tickets/filter")]
        public async Task<IActionResult> GetFilteredTickets([FromBody] TicketFilterDto filter)
        {
            var tickets = await _adminService.GetAllTicketsFilteredAsync(filter);
            return Ok(tickets);
        }

        [HttpGet("tickets/high-priority")]
        public async Task<IActionResult> GetHighPriorityTickets(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var tickets = await _adminService.GetHighPriorityTicketsPagedAsync(pageIndex, pageSize);
            return Ok(tickets);
        }

        [HttpPut("tickets/{ticketId}/high-priority")]
        public async Task<IActionResult> MarkAsHighPriority(
            string ticketId,
            [FromBody] bool isHighPriority)
        {
            var result = await _adminService.MarkTicketAsHighPriorityAsync(ticketId, isHighPriority);

            if (!result)
                return NotFound(new { message = "Ticket not found" });

            return Ok(new { message = "Ticket priority updated" });
        }
    }
}
