using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsShared.DTO.Analytics
{
    public class TicketDistributionDto
    {
        public Dictionary<string, int> Distribution { get; set; }
    }


//    {
//    "distribution": {
//        "Level 1": 45,
//        "Level 2": 32,
//        "Level 3": 28,
//        "Level 4": 15
//    }
//}
}
