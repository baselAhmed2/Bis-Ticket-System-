using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;


namespace TicketsDomain.IRepositories
{
    public interface IUnitOfWork
    {
     
            IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
                where TEntity : BaseEntity<TKey>;

            Task<int> SaveChangesAsync();
        
    }
}