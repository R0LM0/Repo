using System.Linq.Expressions;

namespace Repo.Repository.Specifications
{
    /// <summary>
    /// Defines a specification pattern for querying entities with filtering, includes, ordering, and paging.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <remarks>
    /// The Specification pattern encapsulates query criteria and allows for reusable, composable queries.
    /// Use with IRepo methods like GetBySpecAsync, GetAllBySpecAsync, and GetPagedBySpecAsync.
    /// </remarks>
    public interface ISpecification<T>
    {
        /// <summary>
        /// Gets the filter criteria expression.
        /// </summary>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>
        /// Gets the list of include expressions for related entities.
        /// </summary>
        List<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Gets the list of include strings for related entities (supports nested includes).
        /// </summary>
        List<string> IncludeStrings { get; }

        /// <summary>
        /// Gets the ascending order expression.
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        /// <summary>
        /// Gets the descending order expression.
        /// </summary>
        Expression<Func<T, object>>? OrderByDescending { get; }

        /// <summary>
        /// Gets the number of items to take (for paging).
        /// </summary>
        int Take { get; }

        /// <summary>
        /// Gets the number of items to skip (for paging).
        /// </summary>
        int Skip { get; }

        /// <summary>
        /// Gets a value indicating whether paging is enabled.
        /// </summary>
        bool IsPagingEnabled { get; }

        /// <summary>
        /// Gets or sets a value indicating whether change tracking is enabled.
        /// </summary>
        bool IsTrackingEnabled { get; set; }
    }

    /// <summary>
    /// Base implementation of the <see cref="ISpecification{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <remarks>
    /// Inherit from this class to create custom specifications. Use the protected methods to configure
    /// the specification with criteria, includes, ordering, and paging.
    /// </remarks>
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        /// <summary>
        /// Gets the filter criteria expression.
        /// </summary>
        public Expression<Func<T, bool>>? Criteria { get; private set; }

        /// <summary>
        /// Gets the list of include expressions for related entities.
        /// </summary>
        public List<Expression<Func<T, object>>> Includes { get; } = new();

        /// <summary>
        /// Gets the list of include strings for related entities (supports nested includes).
        /// </summary>
        public List<string> IncludeStrings { get; } = new();

        /// <summary>
        /// Gets the ascending order expression.
        /// </summary>
        public Expression<Func<T, object>>? OrderBy { get; private set; }

        /// <summary>
        /// Gets the descending order expression.
        /// </summary>
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }

        /// <summary>
        /// Gets the number of items to take (for paging).
        /// </summary>
        public int Take { get; private set; }

        /// <summary>
        /// Gets the number of items to skip (for paging).
        /// </summary>
        public int Skip { get; private set; }

        /// <summary>
        /// Gets a value indicating whether paging is enabled.
        /// </summary>
        public bool IsPagingEnabled { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether change tracking is enabled. Default is true.
        /// </summary>
        public bool IsTrackingEnabled { get; set; } = true;

        /// <summary>
        /// Adds filter criteria to the specification.
        /// </summary>
        /// <param name="criteriaExpression">The filter expression.</param>
        protected void AddCriteria(Expression<Func<T, bool>> criteriaExpression)
        {
            Criteria = criteriaExpression;
        }

        /// <summary>
        /// Adds an include expression for a related entity.
        /// </summary>
        /// <param name="includeExpression">Expression specifying the related entity to include.</param>
        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        /// <summary>
        /// Adds an include string for a related entity (supports nested includes like "Orders.Items").
        /// </summary>
        /// <param name="includeString">The include path.</param>
        protected void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        /// <summary>
        /// Adds ascending order to the specification.
        /// </summary>
        /// <param name="orderByExpression">Expression specifying the property to order by.</param>
        protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        /// <summary>
        /// Adds descending order to the specification.
        /// </summary>
        /// <param name="orderByDescExpression">Expression specifying the property to order by descending.</param>
        protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
        {
            OrderByDescending = orderByDescExpression;
        }

        /// <summary>
        /// Applies paging to the specification.
        /// </summary>
        /// <param name="skip">The number of items to skip.</param>
        /// <param name="take">The number of items to take.</param>
        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
    }
}