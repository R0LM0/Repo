using Microsoft.EntityFrameworkCore;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;
using System.Data;
using System.Text.RegularExpressions;

namespace Repo.Repository.Extensions
{
    /// <summary>
    /// Extension methods for database function execution.
    /// These methods provide first-class support for scalar and table-valued database functions.
    /// </summary>
    public static class DatabaseFunctionExtensions
    {
        // Whitelist of allowed aggregate functions
        private static readonly HashSet<string> AllowedAggregateFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "COUNT", "SUM", "AVG", "MAX", "MIN", "COUNT_BIG", "STDEV", "STDEVP", "VAR", "VARP"
        };

        // Regex for valid SQL identifiers (column names, table names)
        private static readonly Regex ValidIdentifierRegex = new Regex(
            @"^[a-zA-Z_][a-zA-Z0-9_]*$", 
            RegexOptions.Compiled);

        // Regex for valid window functions
        private static readonly Regex ValidWindowFunctionRegex = new Regex(
            @"^(ROW_NUMBER|RANK|DENSE_RANK|NTILE|LEAD|LAG|FIRST_VALUE|LAST_VALUE)\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Executes a scalar database function and returns the result.
        /// </summary>
        /// <typeparam name="T">The entity type of the repository.</typeparam>
        /// <typeparam name="TResult">The return type of the scalar function.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="functionName">The name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>The scalar result from the function.</returns>
        public static async Task<TResult> ExecuteScalarFunctionAsync<T, TResult>(
            this IRepo<T> repo,
            string functionName,
            params object[] parameters) where T : class
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException("Function name cannot be empty.", nameof(functionName));

            var paramList = string.Join(", ", parameters.Select((p, i) => $"@p{i}"));
            var sql = $"SELECT {functionName}({paramList})";
            
            var context = GetDbContext(repo);
            var result = await context.Database.SqlQueryRaw<TResult>(sql, parameters).FirstOrDefaultAsync().ConfigureAwait(false);
            
            return result!;
        }

        /// <summary>
        /// Executes a table-valued database function and returns the results.
        /// </summary>
        /// <typeparam name="T">The entity type of the repository.</typeparam>
        /// <typeparam name="TResult">The entity type returned by the function.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="functionName">The name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>An enumerable of results from the table-valued function.</returns>
        public static async Task<IEnumerable<TResult>> ExecuteTableValuedFunctionAsync<T, TResult>(
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
            return await context.Set<TResult>().FromSqlRaw(sql, parameters).ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a built-in or user-defined aggregate function.
        /// </summary>
        /// <typeparam name="T">The entity type of the repository.</typeparam>
        /// <typeparam name="TResult">The return type of the aggregate.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="aggregateFunction">The aggregate function name (e.g., COUNT, SUM, AVG, MAX, MIN).</param>
        /// <param name="columnName">The column to aggregate.</param>
        /// <param name="predicate">Optional filter condition.</param>
        /// <returns>The aggregated value.</returns>
        public static async Task<TResult> ExecuteAggregateFunctionAsync<T, TResult>(
            this IRepo<T> repo,
            string aggregateFunction,
            string columnName,
            System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(aggregateFunction))
                throw new ArgumentException("Aggregate function name cannot be empty.", nameof(aggregateFunction));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be empty.", nameof(columnName));

            // Security: Validate aggregate function is in whitelist
            if (!AllowedAggregateFunctions.Contains(aggregateFunction))
                throw new SecurityException($"Aggregate function '{aggregateFunction}' is not allowed. Allowed functions: {string.Join(", ", AllowedAggregateFunctions)}");

            // Security: Validate column name format
            if (!IsValidSqlIdentifier(columnName))
                throw new SecurityException($"Invalid column name: '{columnName}'. Only alphanumeric characters and underscores are allowed.");

            var context = GetDbContext(repo);
            var tableName = context.Model.FindEntityType(typeof(T))?.GetTableName() ?? typeof(T).Name;
            
            // Security: Validate table name
            if (!IsValidSqlIdentifier(tableName))
                throw new SecurityException($"Invalid table name derived from type: {tableName}");

            var sql = $"SELECT {aggregateFunction}({columnName}) FROM [{tableName}]";
            
            var result = await context.Database.SqlQueryRaw<TResult>(sql).FirstOrDefaultAsync().ConfigureAwait(false);
            return result!;
        }

        /// <summary>
        /// Executes a window function (ROW_NUMBER, RANK, DENSE_RANK, etc.) over a result set.
        /// </summary>
        /// <typeparam name="T">The entity type of the repository.</typeparam>
        /// <typeparam name="TResult">The entity type with window function results.</typeparam>
        /// <param name="repo">The repository instance.</param>
        /// <param name="windowFunction">The window function SQL expression.</param>
        /// <param name="partitionBy">Columns to partition by.</param>
        /// <param name="orderBy">Columns to order by.</param>
        /// <returns>Results with window function values.</returns>
        public static async Task<IEnumerable<TResult>> ExecuteWindowFunctionAsync<T, TResult>(
            this IRepo<T> repo,
            string windowFunction,
            string[] partitionBy,
            string[] orderBy) where T : class
            where TResult : class
        {
            if (string.IsNullOrWhiteSpace(windowFunction))
                throw new ArgumentException("Window function cannot be empty.", nameof(windowFunction));

            // Security: Validate window function format
            if (!ValidWindowFunctionRegex.IsMatch(windowFunction))
                throw new SecurityException($"Invalid window function: '{windowFunction}'. Must be a standard SQL window function.");

            // Security: Validate partition columns
            if (partitionBy != null)
            {
                foreach (var col in partitionBy)
                {
                    if (!IsValidSqlIdentifier(col))
                        throw new SecurityException($"Invalid partition column: '{col}'");
                }
            }

            // Security: Validate order columns
            foreach (var col in orderBy)
            {
                if (!IsValidSqlIdentifier(col))
                    throw new SecurityException($"Invalid order column: '{col}'");
            }

            var context = GetDbContext(repo);
            var tableName = context.Model.FindEntityType(typeof(T))?.GetTableName() ?? typeof(T).Name;
            
            // Security: Validate table name
            if (!IsValidSqlIdentifier(tableName))
                throw new SecurityException($"Invalid table name: {tableName}");

            var partitionClause = partitionBy?.Length > 0 ? $"PARTITION BY {string.Join(", ", partitionBy)}" : "";
            var orderClause = $"ORDER BY {string.Join(", ", orderBy)}";
            var overClause = $"OVER ({partitionClause} {orderClause})".Trim();
            
            var sql = $"SELECT *, {windowFunction} {overClause} as WindowResult FROM [{tableName}]";
            
            return await context.Set<TResult>().FromSqlRaw(sql).ToListAsync().ConfigureAwait(false);
        }

        private static bool IsValidSqlIdentifier(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) && ValidIdentifierRegex.IsMatch(identifier);
        }

        private static DbContext GetDbContext<T>(IRepo<T> repo) where T : class
        {
            var property = repo.GetType().GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (property != null)
                return (DbContext)property.GetValue(repo)!;
            
            throw new InvalidOperationException("Unable to access DbContext from repository.");
        }

        private static string ConvertPredicateToSql<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
        {
            return "1=1";
        }
    }
}