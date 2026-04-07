// -----------------------------------------------------------------------------
// RepoBase.Pagination.cs
// -----------------------------------------------------------------------------
// Partial class: Pagination Operations
// 
// Purpose:
//   Contains pagination-related methods for querying entities in pages,
//   including sorting, filtering, and result wrapping.
//
// Methods:
//   - GetPagedAsync (2 overloads) - Get paged results with optional filtering
//   - ApplySearchFilter - Internal method for search filtering (throws NotImplementedException)
// -----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Repo.Repository.Extensions;
using Repo.Repository.Models;
using System.Linq.Expressions;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region NUEVOS MÉTODOS - Paginación y Filtrado
        /// <summary>
        /// Gets a paginated list of entities with optional search and sorting.
        /// </summary>
        /// <param name="request">Pagination request parameters.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paged result containing entities and metadata.</returns>
        public async Task<PagedResult<T>> GetPagedAsync(PagedRequest request, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = asNoTracking ? Table.AsNoTracking() : Table.AsQueryable();

                // Apply search if SearchTerm exists
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = ApplySearchFilter(query, request.SearchTerm);
                }

                // Apply dynamic ordering
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = query.OrderByDynamic(request.SortBy, request.IsAscending);
                }
                else
                {
                    // Orden por PK o por la primera propiedad si no se especifica
                    var pk = typeof(T).GetProperties().First().Name;
                    query = query.OrderByDynamic(pk, true);
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets a paginated list of entities matching the specified filter expression.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <param name="request">Pagination request parameters.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paged result containing filtered entities and metadata.</returns>
        public async Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>> filter, PagedRequest request, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = asNoTracking ? Table.AsNoTracking().Where(filter) : Table.Where(filter);

                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedAsync con filtro para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Applies a search filter to the query based on the search term.
        /// </summary>
        /// <param name="query">The source query.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>The filtered query.</returns>
        /// <exception cref="NotImplementedException">Always thrown as this method requires implementation.</exception>
        private IQueryable<T> ApplySearchFilter(IQueryable<T> query, string searchTerm)
        {
            // This method is not implemented. Dynamic text search across entity properties
            // requires either System.Linq.Dynamic.Core or explicit property configuration.
            // Consider using specifications (ISpecification<T>) for type-safe filtering.
            throw new NotImplementedException(
                "ApplySearchFilter is not implemented. Use specification-based filtering with ISpecification<T> instead. " +
                "For dynamic search requirements, consider implementing a custom search strategy specific to your entity.");
        }
        #endregion
    }
}
