using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsDomain.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        

        // Navigation
        public ICollection<DoctorSubject> DoctorSubjects { get; set; }

    }
}
