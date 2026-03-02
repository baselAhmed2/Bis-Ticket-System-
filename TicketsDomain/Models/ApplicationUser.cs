using Microsoft.AspNetCore.Identity;

namespace TicketsDomain.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string? Program { get; set; } // BIS, FMI, CS — null for SuperAdmin

        // Optional properties used by AdminService / DataInitializer
        public string? SSN { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<DoctorSubject> DoctorSubjects { get; set; }
    }
}
