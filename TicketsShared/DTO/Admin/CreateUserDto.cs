using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Admin
{
    public class CreateUserDto
    {
        public string Id { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }         // "Student" / "Doctor"
        public string Program { get; set; }      // "BIS" / "FMI" / "CS"
    }
}
