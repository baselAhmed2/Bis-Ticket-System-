using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;

namespace TicketsDomain.Specifications
{
    public interface ISpecification
    {
        public interface ISpecification<TEntity, TKey>
            where TEntity : BaseEntity<TKey>
        {
            Expression<Func<TEntity, bool>>? Criteria { get; }
            List<Expression<Func<TEntity, object>>> Includes { get; }
            Expression<Func<TEntity, object>>? OrderBy { get; }
            Expression<Func<TEntity, object>>? OrderByDescending { get; }
            int Take { get; }
            int Skip { get; }
            bool IsPagingEnabled { get; }
        }
    }
}