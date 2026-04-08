using System.Linq.Expressions;

namespace Repo.Repository.Specifications
{
    /// <summary>
    /// Provides composite operations for specifications (AND, OR, NOT).
    /// Enables combining specifications with logical operators.
    /// </summary>
    public static class CompositeSpecification
    {
        /// <summary>
        /// Combines two specifications with AND logic.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="left">The left specification.</param>
        /// <param name="right">The right specification.</param>
        /// <returns>A new specification that matches when both specifications match.</returns>
        public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) where T : class
        {
            return new AndSpecification<T>(left, right);
        }

        /// <summary>
        /// Combines two specifications with OR logic.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="left">The left specification.</param>
        /// <param name="right">The right specification.</param>
        /// <returns>A new specification that matches when either specification matches.</returns>
        public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) where T : class
        {
            return new OrSpecification<T>(left, right);
        }

        /// <summary>
        /// Negates a specification with NOT logic.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="spec">The specification to negate.</param>
        /// <returns>A new specification that matches when the original does not match.</returns>
        public static ISpecification<T> Not<T>(this ISpecification<T> spec) where T : class
        {
            return new NotSpecification<T>(spec);
        }
    }

    /// <summary>
    /// Specification that combines two specifications with AND logic.
    /// </summary>
    public class AndSpecification<T> : ISpecification<T> where T : class
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;

        public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            _left = left;
            _right = right;
        }

        public Expression<Func<T, bool>>? Criteria => CombineExpressions(_left.Criteria, _right.Criteria, Expression.AndAlso);
        public List<Expression<Func<T, object>>> Includes => _left.Includes.Concat(_right.Includes).ToList();
        public List<string> IncludeStrings => _left.IncludeStrings.Concat(_right.IncludeStrings).ToList();
        public Expression<Func<T, object>>? OrderBy => _left.OrderBy ?? _right.OrderBy;
        public Expression<Func<T, object>>? OrderByDescending => _left.OrderByDescending ?? _right.OrderByDescending;
        public int Take => _left.IsPagingEnabled ? _left.Take : _right.Take;
        public int Skip => _left.IsPagingEnabled ? _left.Skip : _right.Skip;
        public bool IsPagingEnabled => _left.IsPagingEnabled || _right.IsPagingEnabled;
        public bool IsTrackingEnabled { get; set; } = true;
        public bool UseSplitQuery { get; set; }

        private static Expression<Func<T, bool>>? CombineExpressions(
            Expression<Func<T, bool>>? left,
            Expression<Func<T, bool>>? right,
            Func<Expression, Expression, BinaryExpression> combine)
        {
            if (left == null) return right;
            if (right == null) return left;

            var parameter = Expression.Parameter(typeof(T), "x");
            var leftVisitor = new ReplaceParameterVisitor(left.Parameters[0], parameter);
            var rightVisitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);

            var leftBody = leftVisitor.Visit(left.Body);
            var rightBody = rightVisitor.Visit(right.Body);
            var combined = combine(leftBody, rightBody);

            return Expression.Lambda<Func<T, bool>>(combined, parameter);
        }
    }

    /// <summary>
    /// Specification that combines two specifications with OR logic.
    /// </summary>
    public class OrSpecification<T> : ISpecification<T> where T : class
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;

        public OrSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            _left = left;
            _right = right;
        }

        public Expression<Func<T, bool>>? Criteria => CombineExpressions(_left.Criteria, _right.Criteria, Expression.OrElse);
        public List<Expression<Func<T, object>>> Includes => _left.Includes.Concat(_right.Includes).ToList();
        public List<string> IncludeStrings => _left.IncludeStrings.Concat(_right.IncludeStrings).ToList();
        public Expression<Func<T, object>>? OrderBy => _left.OrderBy ?? _right.OrderBy;
        public Expression<Func<T, object>>? OrderByDescending => _left.OrderByDescending ?? _right.OrderByDescending;
        public int Take => _left.IsPagingEnabled ? _left.Take : _right.Take;
        public int Skip => _left.IsPagingEnabled ? _left.Skip : _right.Skip;
        public bool IsPagingEnabled => _left.IsPagingEnabled || _right.IsPagingEnabled;
        public bool IsTrackingEnabled { get; set; } = true;
        public bool UseSplitQuery { get; set; }

        private static Expression<Func<T, bool>>? CombineExpressions(
            Expression<Func<T, bool>>? left,
            Expression<Func<T, bool>>? right,
            Func<Expression, Expression, BinaryExpression> combine)
        {
            if (left == null) return right;
            if (right == null) return left;

            var parameter = Expression.Parameter(typeof(T), "x");
            var leftVisitor = new ReplaceParameterVisitor(left.Parameters[0], parameter);
            var rightVisitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);

            var leftBody = leftVisitor.Visit(left.Body);
            var rightBody = rightVisitor.Visit(right.Body);
            var combined = combine(leftBody, rightBody);

            return Expression.Lambda<Func<T, bool>>(combined, parameter);
        }
    }

    /// <summary>
    /// Specification that negates another specification.
    /// </summary>
    public class NotSpecification<T> : ISpecification<T> where T : class
    {
        private readonly ISpecification<T> _spec;

        public NotSpecification(ISpecification<T> spec)
        {
            _spec = spec;
        }

        public Expression<Func<T, bool>>? Criteria => _spec.Criteria != null 
            ? Expression.Lambda<Func<T, bool>>(
                Expression.Not(_spec.Criteria.Body), 
                _spec.Criteria.Parameters)
            : null;

        public List<Expression<Func<T, object>>> Includes => _spec.Includes;
        public List<string> IncludeStrings => _spec.IncludeStrings;
        public Expression<Func<T, object>>? OrderBy => _spec.OrderBy;
        public Expression<Func<T, object>>? OrderByDescending => _spec.OrderByDescending;
        public int Take => _spec.Take;
        public int Skip => _spec.Skip;
        public bool IsPagingEnabled => _spec.IsPagingEnabled;
        public bool IsTrackingEnabled { get; set; } = true;
        public bool UseSplitQuery { get; set; }
    }

    /// <summary>
    /// Helper class to replace parameter expressions.
    /// </summary>
    internal class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
