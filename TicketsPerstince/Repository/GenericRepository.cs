using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsDomain.Specifications;
using TicketsPerstince.Data.DbContexts;
 
using static TicketsDomain.Specifications.ISpecification;

namespace TicketsPerstince.Repository
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
    {
        private readonly ApplicationDbContext _dbContext;

        public GenericRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbContext.Set<TEntity>().ToListAsync();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(
            ISpecification<TEntity, TKey>? spec)
        {
            return await SpecificationEvaluator
                .CreateSpecification<TEntity, TKey>(
                    _dbContext.Set<TEntity>(), spec)
                .ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(TKey id)
        {
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }

        public Task UpdateAsync(TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(TKey id)
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync(id);
            if (entity != null)
                _dbContext.Set<TEntity>().Remove(entity);
        }

        public Task<int> CountAsync(ISpecification<TEntity, TKey> spec)
        {
            return SpecificationEvaluator
                .CreateSpecification<TEntity, TKey>(
                    _dbContext.Set<TEntity>(), spec)
                .CountAsync();
        }
    }
}
