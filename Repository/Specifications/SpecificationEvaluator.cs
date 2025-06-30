using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Repo.Repository.Specifications
{
    public class SpecificationEvaluator<T> where T : class
    {
        public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> spec)
        {
            var query = inputQuery;

            // Aplicar criterios de filtrado
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Aplicar ordenamiento
            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
            }

            // Aplicar paginación
            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }

            // Aplicar includes
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
            query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

            return query;
        }
    }
}