namespace TicketsShared.DTO.Tickets
{
    public class CreateTicketDto
    {
        public required string Title { get; set; }
        public required string Body { get; set; }
        public int GroupNumber { get; set; }
        public required string SubjectId { get; set; }
        public required string DoctorId { get; set; }
        public int Level { get; set; }
        public int Term { get; set; }
                    
    }
}