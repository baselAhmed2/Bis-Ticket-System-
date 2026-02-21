using System.Net.Sockets;

namespace TicketsDomain.Models
{
    public class Subject : BaseEntity<string>
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int Term { get; set; }
        public string Program { get; set; } // BIS, FMI, CS

        // Navigation
        public ICollection<DoctorSubject> DoctorSubjects { get; set; }
        public ICollection<Ticket> Tickets { get; set; }
    }
}

