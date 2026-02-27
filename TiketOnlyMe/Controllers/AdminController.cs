using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Admin;

namespace TiketApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,SubAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ITicketService _ticketService;
        private readonly IDoctorService _doctorService;

        public AdminController(IAdminService adminService, ITicketService ticketService, IDoctorService doctorService)
        {
            _adminService = adminService;
            _ticketService = ticketService;
            _doctorService = doctorService;
        }

        /// <summary>
        /// يرجع الـ Program بتاع الـ SubAdmin من الـ JWT Claims.
        /// SuperAdmin يرجع null (يشوف كل البرامج).
        /// </summary>
        private string? GetCurrentProgram()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "SuperAdmin") return null;

            return User.FindFirstValue("Program");
        }

        // =====================
        // User Management
        // =====================
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? role = null)
        {
            var program = GetCurrentProgram();
            var users = await _adminService.GetAllUsersAsync(pageIndex, pageSize, searchTerm, program, role);
            return Ok(users);
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var user = await _adminService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // SubAdmin يشوف بس users البرنامج بتاعه
            var program = GetCurrentProgram();
            if (program != null && user.Program != program)
                return Forbid();

            return Ok(user);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            // SubAdmin يقدر يضيف بس في البرنامج بتاعه
            var program = GetCurrentProgram();
            if (program != null && dto.Program != program)
                return Forbid();

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
            // تحقق الأول إن الـ User في نفس البرنامج
            var program = GetCurrentProgram();
            if (program != null)
            {
                var user = await _adminService.GetUserByIdAsync(userId);
                if (user == null) return NotFound(new { message = "User not found" });
                if (user.Program != program) return Forbid();
            }

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
            var program = GetCurrentProgram();
            var subjects = await _adminService.GetAllSubjectsAsync(program);
            return Ok(subjects);
        }

        [HttpPost("subjects")]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDto dto)
        {
            var program = GetCurrentProgram();
            if (program != null && dto.Program != program)
                return Forbid();

            try
            {
                var subject = await _adminService.CreateSubjectAsync(dto);
                return CreatedAtAction(nameof(GetAllSubjects), null, subject);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin/SubAdmin يضيف نفسه كمدرس لمادة (يظهر في قائمة الدكاترة عند إنشاء التذكرة)
        /// </summary>
        [HttpPost("subjects/{subjectId}/assign-self")]
        public async Task<IActionResult> AssignSelfToSubject(string subjectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var subject = await _adminService.GetAllSubjectsAsync(GetCurrentProgram());
            var sub = subject.FirstOrDefault(s => s.Id == subjectId);
            if (sub == null)
                return NotFound(new { message = "Subject not found" });

            var program = GetCurrentProgram();
            if (program != null && sub.Program != program)
                return Forbid();

            var result = await _adminService.AssignAdminToSubjectAsync(userId, subjectId);
            return Ok(new { message = "Assigned successfully" });
        }

        /// <summary>
        /// Admin/SubAdmin يزيل نفسه من مادة
        /// </summary>
        [HttpDelete("subjects/{subjectId}/unassign-self")]
        public async Task<IActionResult> UnassignSelfFromSubject(string subjectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _adminService.RemoveAdminFromSubjectAsync(userId, subjectId);
            if (!result)
                return NotFound(new { message = "Assignment not found" });
            return Ok(new { message = "Unassigned successfully" });
        }

        /// <summary>
        /// تذاكر المواد اللي الأدمن مدرس فيها (عندما يكون Admin دكتور أيضاً)
        /// </summary>
        [HttpGet("my-doctor-tickets")]
        public async Task<IActionResult> GetMyDoctorTickets(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var tickets = await _ticketService.GetByDoctorIdPagedAsync(userId, pageIndex, pageSize);
            return Ok(tickets);
        }

        /// <summary>
        /// إحصائيات تذاكر المواد اللي الأدمن مدرس فيها
        /// </summary>
        [HttpGet("my-doctor-stats")]
        public async Task<IActionResult> GetMyDoctorStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var stats = await _doctorService.GetDoctorStatsAsync(userId);
            return Ok(stats);
        }

        // =====================
        // Ticket Monitoring
        // =====================
        [HttpPost("tickets/filter")]
        public async Task<IActionResult> GetFilteredTickets([FromBody] TicketFilterDto filter)
        {
            // SubAdmin يشوف بس tickets البرنامج بتاعه
            var program = GetCurrentProgram();
            if (program != null)
                filter.Program = program;

            var tickets = await _adminService.GetAllTicketsFilteredAsync(filter);
            return Ok(tickets);
        }

        [HttpGet("tickets/high-priority")]
        public async Task<IActionResult> GetHighPriorityTickets(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var program = GetCurrentProgram();
            var tickets = await _adminService.GetHighPriorityTicketsPagedAsync(pageIndex, pageSize, program);
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

