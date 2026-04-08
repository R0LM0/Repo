using System.Linq.Expressions;

namespace Repo.Repository.Specifications
{
    /// <summary>
    /// Implementation of a specification with projection support.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    public class Specification<T, TResult> : ISpecification<T, TResult> where T : class
    {
        private readonly ISpecification<T> _baseSpec;

        /// <summary>
        /// Creates a new specification with projection based on an existing specification.
        /// </summary>
        /// <param name="baseSpec">The base specification to copy settings from.</param>
        /// <param name="selector">The projection expression.</param>
        public Specification(ISpecification<T> baseSpec, Expression<Func<T, TResult>> selector)
        {
            _baseSpec = baseSpec;
            Selector = selector;
        }

        /// <inheritdoc />
        public Expression<Func<T, bool>>? Criteria => _baseSpec.Criteria;

        /// <inheritdoc />
        public List<Expression<Func<T, object>>> Includes => _baseSpec.Includes;

        /// <inheritdoc />
        public List<string> IncludeStrings => _baseSpec.IncludeStrings;

        /// <inheritdoc />
        public Expression<Func<T, object>>? OrderBy => _baseSpec.OrderBy;

        /// <inheritdoc />
        public Expression<Func<T, object>>? OrderByDescending => _baseSpec.OrderByDescending;

        /// <inheritdoc />
        public int Take => _baseSpec.Take;

        /// <inheritdoc />
        public int Skip => _baseSpec.Skip;

        /// <inheritdoc />
        public bool IsPagingEnabled => _baseSpec.IsPagingEnabled;

        /// <inheritdoc />
        public bool IsTrackingEnabled { get => _baseSpec.IsTrackingEnabled; set => _baseSpec.IsTrackingEnabled = value; }

        /// <inheritdoc />
        public bool UseSplitQuery { get => _baseSpec.UseSplitQuery; set => _baseSpec.UseSplitQuery = value; }

        /// <inheritdoc />
        public Expression<Func<T, TResult>>? Selector { get; }
    }
}
