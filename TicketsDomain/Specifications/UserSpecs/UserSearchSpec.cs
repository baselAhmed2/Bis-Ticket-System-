using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;

namespace TicketsDomain.Specifications.UserSpecs
{
    public class UserSearchSpec
    {
        public static IQueryable<ApplicationUser> ApplySearch(
            IQueryable<ApplicationUser> query, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return query;

            var lowerSearch = searchTerm.ToLower();

            return query.Where(u =>
                u.Id.ToLower().Contains(lowerSearch) ||
                u.UserName.ToLower().Contains(lowerSearch) ||
                u.Name.ToLower().Contains(lowerSearch));
        }

        public static IQueryable<ApplicationUser> ApplyPaging(
            IQueryable<ApplicationUser> query, int pageIndex, int pageSize)
        {
            return query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);
        }
    }
}
