// -----------------------------------------------------------------------------
// RepoBase.Specifications.cs
// -----------------------------------------------------------------------------
// Partial class: Specification Pattern Operations
// 
// Purpose:
//   Contains methods for querying entities using the Specification pattern,
//   enabling complex query composition and reuse.
//
// Dependencies:
//   - ISpecification<T> - Interface for specification definitions
//   - SpecificationEvaluator<T> - Evaluates specifications against queries
//
// Methods:
//   - GetBySpecAsync - Get single entity by specification
//   - GetAllBySpecAsync - Get all entities matching specification
//   - GetPagedBySpecAsync - Get paged results by specification
//   - CountBySpecAsync - Count entities matching specification
// -----------------------------------------------------------------------------

using Repo.Repository.Specifications;
using Repo.Repository.Models;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region NUEVOS MÉTODOS - Especificaciones
        /// <summary>
        /// Gets a single entity matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching entity, or null if not found.</returns>
        public async Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets all entities matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching entities.</returns>
        public async Task<IEnumerable<T>> GetAllBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets a paginated list of entities matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <param name="request">Pagination request parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paged result containing matching entities.</returns>
        public async Task<PagedResult<T>> GetPagedBySpecAsync(ISpecification<T> spec, PagedRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Counts entities matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of matching entities.</returns>
        public async Task<int> CountBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en CountBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion
    }
}
