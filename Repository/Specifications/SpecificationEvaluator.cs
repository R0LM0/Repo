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

            // Apply pagination
            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }

            // Aplicar includes
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
            query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

            // Apply split query for multiple includes to avoid Cartesian explosion
            // Automatically enable when UseSplitQuery is true or when more than one include is present
            var totalIncludes = spec.Includes.Count + spec.IncludeStrings.Count;
            if (spec.UseSplitQuery || totalIncludes > 1)
            {
                query = query.AsSplitQuery();
            }

            // Apply tracking configuration
            if (!spec.IsTrackingEnabled)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        /// <summary>
        /// Evaluates a specification with projection and returns a queryable of the projected type.
        /// </summary>
        /// <typeparam name="TResult">The projected result type.</typeparam>
        /// <param name="inputQuery">The input queryable.</param>
        /// <param name="spec">The specification with projection.</param>
        /// <returns>A queryable of the projected type.</returns>
        public static IQueryable<TResult> GetQuery<TResult>(IQueryable<T> inputQuery, ISpecification<T, TResult> spec)
        {
            var query = GetQuery(inputQuery, (ISpecification<T>)spec);

            // Apply projection if selector is defined
            if (spec.Selector != null)
            {
                return query.Select(spec.Selector);
            }

            throw new InvalidOperationException("Selector must be defined when using projected specification.");
        }
    }
}