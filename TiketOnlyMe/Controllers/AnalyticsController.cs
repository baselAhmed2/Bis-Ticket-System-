using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketsServiesAbstraction.IServices;

namespace TiketApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,SubAdmin")] // ✅ FIX: كان "Admin"
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminAnalytics()
        {
            var analytics = await _analyticsService.GetAdminAnalyticsAsync();
            return Ok(analytics);
        }

        [HttpGet("top-doctors")]
        public async Task<IActionResult> GetTopDoctors([FromQuery] int count = 10)
        {
            var doctors = await _analyticsService.GetDoctorsByTicketCountAsync(count);
            return Ok(doctors);
        }

        [HttpGet("top-subjects")]
        public async Task<IActionResult> GetTopSubjects([FromQuery] int count = 10)
        {
            var subjects = await _analyticsService.GetSubjectsByTicketCountAsync(count);
            return Ok(subjects);
        }
    }
}
