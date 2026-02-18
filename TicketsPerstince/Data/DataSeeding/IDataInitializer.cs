using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsPerstince.Data.DataSeeding
{
    public interface IDataInitializer
    {
        Task InitializeAsync();
    }
}
