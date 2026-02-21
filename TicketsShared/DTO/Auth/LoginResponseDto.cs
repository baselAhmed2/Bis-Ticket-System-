    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Auth
{
    public class LoginResponseDto
    {
        public required string Token { get; set; }
        public required string UserId { get; set; }
        public required string Name { get; set; }
        public required string Role { get; set; }
        public string? Program { get; set; } // BIS, FMI, CS — null for SuperAdmin
    }
}
