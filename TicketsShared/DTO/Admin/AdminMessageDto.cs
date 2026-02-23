    namespace TicketsShared.DTO.Admin
{
    public class AdminMessageDto
    {
        public int MessageId { get; set; }
        public string Body { get; set; }
        public DateTime SentAt { get; set; }
        public string SenderName { get; set; }
        public string SenderId { get; set; }

        // Ticket Info
        public string TicketId { get; set; }
        public string TicketTitle { get; set; }
        public string StudentName { get; set; }
        public string DoctorName { get; set; }
    }
}