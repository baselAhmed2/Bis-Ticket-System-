using Microsoft.AspNetCore.Identity;

namespace TicketsDomain.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string? Program { get; set; } // BIS, FMI, CS — null for SuperAdmin

        // Navigation
        public ICollection<DoctorSubject> DoctorSubjects { get; set; }
    }
}
