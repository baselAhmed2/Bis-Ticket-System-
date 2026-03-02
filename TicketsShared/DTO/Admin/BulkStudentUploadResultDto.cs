using System.Collections.Generic;

namespace TicketsShared.DTO.Admin
{
    public class BulkStudentUploadResultDto
    {
        public int TotalRows { get; set; }
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
