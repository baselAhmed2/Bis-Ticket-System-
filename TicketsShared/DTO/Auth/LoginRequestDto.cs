using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Auth
{
        public class LoginRequestDto
        {
            public required string Username { get; set; }  // ID الكلية
            public required string Password { get; set; }  // SSN
        }
}
