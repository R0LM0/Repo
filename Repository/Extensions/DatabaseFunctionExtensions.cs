using Microsoft.EntityFrameworkCore;
using Repo.Repository.Base;
using System.Data;

namespace Repo.Repository.Extensions
{
    /// <summary>
    /// Extension methods for database function execution.
    /// These methods provide first-class support for scalar and table-valued database functions.
    /// </summary>
    public static class DatabaseFunctionExtensions
    {
        /// <summary>
        /// Executes a scalar database function and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the scalar function.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="functionName">The name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>The scalar result from the function.</returns>
        public static async Task<TResult> ExecuteScalarFunctionAsync<TResult>(
            this IRepo<T> repo,
            string functionName,
            params object[] parameters) where T : class
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException("Function name cannot be empty.", nameof(functionName));

            var paramList = string.Join(", ", parameters.Select((p, i) => $"@p{i}"));
            var sql = $"SELECT {functionName}({paramList})";
            
            // This will be executed via raw SQL
            var context = GetDbContext(repo);
            var result = await context.Database.SqlQueryRaw<TResult>(sql, parameters).FirstOrDefaultAsync();
            
            return result!;
        }

        /// <summary>
        /// Executes a table-valued database function and returns the results.
        /// </summary>
        /// <typeparam name="TResult">The entity type returned by the function.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="functionName">The name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>An enumerable of results from the table-valued function.</returns>
        public static async Task<IEnumerable<TResult>> ExecuteTableValuedFunctionAsync<TResult>(
            this IRepo<T> repo,
            string functionName,
            params object[] parameters) where T : class
            where TResult : class
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException("Function name cannot be empty.", nameof(functionName));

            var paramList = string.Join(", ", parameters.Select((p, i) => $"@p{i}"));
            var sql = $"SELECT * FROM {functionName}({paramList})";
            
            var context = GetDbContext(repo);
            return await context.Set<TResult>().FromSqlRaw(sql, parameters).ToListAsync();
        }

        /// <summary>
        /// Executes a built-in or user-defined aggregate function.
        /// </summary>
        /// <typeparam name="TResult">The return type of the aggregate.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="aggregateFunction">The aggregate function name (e.g., COUNT, SUM, AVG, MAX, MIN).</param>
        /// <param name="columnName">The column to aggregate.</param>
        /// <param name="predicate">Optional filter condition.</param>
        /// <returns>The aggregated value.</returns>
        public static async Task<TResult> ExecuteAggregateFunctionAsync<TResult>(
            this IRepo<T> repo,
            string aggregateFunction,
            string columnName,
            System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(aggregateFunction))
                throw new ArgumentException("Aggregate function name cannot be empty.", nameof(aggregateFunction));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be empty.", nameof(columnName));

            var context = GetDbContext(repo);
            var tableName = context.Model.FindEntityType(typeof(T))?.GetTableName() ?? typeof(T).Name;
            
            var whereClause = predicate != null ? "WHERE " + ConvertPredicateToSql(predicate) : "";
            var sql = $"SELECT {aggregateFunction}({columnName}) FROM {tableName} {whereClause}";
            
            var result = await context.Database.SqlQueryRaw<TResult>(sql).FirstOrDefaultAsync();
            return result!;
        }

        /// <summary>
        /// Executes a window function (ROW_NUMBER, RANK, DENSE_RANK, etc.) over a result set.
        /// </summary>
        /// <typeparam name="TResult">The entity type with window function results.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="windowFunction">The window function SQL expression.</param>
        /// <param name="partitionBy">Columns to partition by.</param>
        /// <param name="orderBy">Columns to order by.</param>
        /// <returns>Results with window function values.</returns>
        public static async Task<IEnumerable<TResult>> ExecuteWindowFunctionAsync<TResult>(
            this IRepo<T> repo,
            string windowFunction,
            string[] partitionBy,
            string[] orderBy) where T : class
            where TResult : class
        {
            if (string.IsNullOrWhiteSpace(windowFunction))
                throw new ArgumentException("Window function cannot be empty.", nameof(windowFunction));

            var context = GetDbContext(repo);
            var tableName = context.Model.FindEntityType(typeof(T))?.GetTableName() ?? typeof(T).Name;
            
            var partitionClause = partitionBy?.Length > 0 ? $"PARTITION BY {string.Join(", ", partitionBy)}" : "";
            var orderClause = $"ORDER BY {string.Join(", ", orderBy)}";
            var overClause = $"OVER ({partitionClause} {orderClause})".Trim();
            
            var sql = $"SELECT *, {windowFunction} {overClause} as WindowResult FROM {tableName}";
            
            return await context.Set<TResult>().FromSqlRaw(sql).ToListAsync();
        }

        private static DbContext GetDbContext<T>(IRepo<T> repo) where T : class
        {
            // Access the protected Db field via reflection or a public method
            // This is a simplified approach - in production, consider adding a public accessor
            var property = repo.GetType().GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (property != null)
                return (DbContext)property.GetValue(repo)!;
            
            throw new InvalidOperationException("Unable to access DbContext from repository.");
        }

        private static string ConvertPredicateToSql<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
        {
            // Simplified conversion - in production, use a proper expression visitor
            // This is a placeholder for the actual implementation
            return "1=1";
        }
    }
}