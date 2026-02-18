using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;
using TicketsDomain.Specifications;

namespace TicketsDomain.IRepositories
{
  
        public interface IGenericRepository<TEntity, TKey>
            where TEntity : BaseEntity<TKey>
        {
            Task AddAsync(TEntity entity);
            Task<IEnumerable<TEntity>> GetAllAsync();
            Task<IEnumerable<TEntity>> GetAllAsync(ISpecification.ISpecification<TEntity, TKey>? spec);
            Task<TEntity?> GetByIdAsync(TKey id);
            Task UpdateAsync(TEntity entity);
            Task DeleteAsync(TKey id);
            Task<int> CountAsync(ISpecification.ISpecification<TEntity, TKey> spec);
        }
    }
