namespace TicketsDomain.Models
{
    public class Message : BaseEntity<int>
    {
       
        public string Body { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsHighPriority { get; set; } = false;

        // Relations
        public string TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
    }
}
