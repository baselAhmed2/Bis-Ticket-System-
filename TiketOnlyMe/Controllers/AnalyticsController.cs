using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketsServiesAbstraction.IServices;

namespace TiketApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,SubAdmin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminAnalytics([FromQuery] int? period = null)
        {
            var analytics = await _analyticsService.GetAdminAnalyticsAsync(period);
            return Ok(analytics);
        }

        [HttpGet("top-doctors")]
        public async Task<IActionResult> GetTopDoctors(
            [FromQuery] int count = 10,
            [FromQuery] int? level = null)
        {
            var doctors = await _analyticsService.GetDoctorsByTicketCountAsync(count, level);
            return Ok(doctors);
        }

        [HttpGet("top-subjects")]
        public async Task<IActionResult> GetTopSubjects(
            [FromQuery] int count = 10,
            [FromQuery] int? level = null)
        {
            var subjects = await _analyticsService.GetSubjectsByTicketCountAsync(count, level);
            return Ok(subjects);
        }
    }
}
