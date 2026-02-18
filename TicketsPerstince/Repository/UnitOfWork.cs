using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsPerstince.Data.DbContexts;


namespace TicketsPerstince.Repository
{

        public class UnitOfWork : IUnitOfWork 
    {
            public ApplicationDbContext DbContext { get; }
            private readonly Dictionary<string, object> _repositories = new();

            public UnitOfWork(ApplicationDbContext dbContext)
            {
                DbContext = dbContext;
            }

            public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
                where TEntity : BaseEntity<TKey>
            {
                var entityType = typeof(TEntity);

                if (_repositories.TryGetValue(entityType.FullName!, out object? repo))
                    return (IGenericRepository<TEntity, TKey>)repo!;

                var repoInstance = new GenericRepository<TEntity, TKey>(DbContext);
                _repositories.Add(entityType.FullName!, repoInstance);
                return repoInstance;
            }

            public async Task<int> SaveChangesAsync()
                => await DbContext.SaveChangesAsync();
        }

    }

