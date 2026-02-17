using System.Net.Sockets;

namespace TicketsDomain.Models
{
    public class Subject
    {
        public string Id { get; set; }      // "BIS123" / "HU12"
        public string Name { get; set; }
        public int Level { get; set; }      // المادة دي في سنة كام

        // Navigation
        public ICollection<DoctorSubject> DoctorSubjects { get; set; }
        public ICollection<Ticket> Tickets { get; set; }
    }
}