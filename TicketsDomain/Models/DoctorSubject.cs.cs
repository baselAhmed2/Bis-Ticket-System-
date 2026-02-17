using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsDomain.Models
{
    public class DoctorSubject
    {
        public string DoctorId { get; set; }
        public ApplicationUser Doctor { get; set; }

        public string SubjectId { get; set; }
        public Subject Subject { get; set; }
    }
}
