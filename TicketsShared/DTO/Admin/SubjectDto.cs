using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Admin
{
    public class SubjectDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Term { get; set; }
        public string Program { get; set; }
    }
}