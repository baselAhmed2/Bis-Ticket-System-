using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketsDomain.Models;
using TicketsDomain.Specifications;

namespace TicketsPerstince.Repository
{
    public class SpecificationEvaluator
    {
        
            public static IQueryable<TEntity> CreateSpecification<TEntity, TKey>(
                IQueryable<TEntity> inputQuery, ISpecification.ISpecification<TEntity, TKey>? spec)
                where TEntity : BaseEntity<TKey>
            {
                var query = inputQuery;

                if (spec is null) return query;

                // 1️⃣ Where
                if (spec.Criteria is not null)
                    query = query.Where(spec.Criteria);

                // 2️⃣ Includes
                query = spec.Includes.Aggregate(query,
                    (current, include) => current.Include(include));

                // 3️⃣ OrderBy
                if (spec.OrderBy is not null)
                    query = query.OrderBy(spec.OrderBy);

                if (spec.OrderByDescending is not null)
                    query = query.OrderByDescending(spec.OrderByDescending);

                // 4️⃣ Paging
                if (spec.IsPagingEnabled)
                    query = query.Skip(spec.Skip).Take(spec.Take);

                return query;
            }
        }
    }
    
