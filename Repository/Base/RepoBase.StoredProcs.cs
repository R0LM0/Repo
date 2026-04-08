// -----------------------------------------------------------------------------
// RepoBase.StoredProcs.cs
// -----------------------------------------------------------------------------
// Partial class: Stored Procedure and Function Operations
// 
// Purpose:
//   Contains methods for executing stored procedures and database functions
//   with security validation via whitelist.
//
// Security:
//   - All procedure/function names are validated against IStoredProcedureWhitelist
//   - If no whitelist is configured, all names are allowed (backward compatibility)
//   - Throws SecurityException for non-whitelisted names
//
// Methods:
//   - ValidateStoredProcedureName - Internal validation helper
//   - ExecuteStoredProcedureAsync - Execute stored procedure returning entities
//   - ExecuteStoredProcedureNonQueryAsync - Execute stored procedure without results
//   - ExecuteScalarFunctionAsync - Execute scalar-valued function
//   - ExecuteTableValuedFunctionAsync - Execute table-valued function
// -----------------------------------------------------------------------------

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repo.Repository.Exceptions;
using Repo.Repository.Security;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region NEW METHODS - Stored Procedures and Functions
        /// <summary>
        /// Validates that a stored procedure or function name is allowed by the whitelist.
        /// If no whitelist is configured, validation passes for backward compatibility.
        /// </summary>
        /// <param name="name">The name to validate.</param>
        /// <exception cref="SecurityException">Thrown when the name is not in the whitelist and validation is required.</exception>
        protected virtual void ValidateStoredProcedureName(string name)
        {
            // If no whitelist is configured, allow all (backward compatibility)
            if (Whitelist == null)
            {
                return;
            }

            // Validate against whitelist
            if (!Whitelist.IsAllowed(name))
            {
                throw SecurityException.ProcedureNotWhitelisted(name);
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns the results as entities.
        /// </summary>
        /// <typeparam name="TResult">The type of the result entities.</typeparam>
        /// <param name="storedProcedure">The stored procedure name or SQL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="parameters">Parameters for the stored procedure.</param>
        /// <returns>A list of entities returned by the stored procedure.</returns>
        /// <exception cref="ArgumentException">Thrown when stored procedure name is empty.</exception>
        public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters) where TResult : class
        {
            if (string.IsNullOrWhiteSpace(storedProcedure))
                throw new ArgumentException("Stored procedure name cannot be empty.", nameof(storedProcedure));

            // Validate against whitelist
            ValidateStoredProcedureName(storedProcedure);

            try
            {
                return await Db.Set<TResult>().FromSqlRaw(storedProcedure, parameters).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing stored procedure {Procedure} for {Entity}", storedProcedure, typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Executes a stored procedure without returning results (non-query).
        /// </summary>
        /// <param name="storedProcedure">The stored procedure name or SQL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="parameters">Parameters for the stored procedure.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentException">Thrown when stored procedure name is empty.</exception>
        public async Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedure))
                throw new ArgumentException("Stored procedure name cannot be empty.", nameof(storedProcedure));

            // Validate against whitelist
            ValidateStoredProcedureName(storedProcedure);

            try
            {
                // If the stored procedure includes the word EXEC or @, treat it as direct SQL
                if (storedProcedure.Contains("EXEC") || storedProcedure.Contains("@"))
                {
                    return await Db.Database.ExecuteSqlRawAsync(storedProcedure, parameters, cancellationToken);
                }
                else
                {
                    // If it's just the SP name, build the call
                    var paramPlaceholders = string.Join(", ", parameters.OfType<SqlParameter>().Select(p => p.ParameterName));
                    var fullCommand = $"EXEC {storedProcedure} {paramPlaceholders}";
                    return await Db.Database.ExecuteSqlRawAsync(fullCommand, parameters, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing stored procedure (non query) {Procedure} for {Entity}", storedProcedure, typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Executes a scalar-valued function and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="functionName">The function name.</param>
        /// <param name="parameters">Parameters for the function.</param>
        /// <returns>The scalar result.</returns>
        /// <exception cref="ArgumentException">Thrown when function name is empty.</exception>
        public async Task<TResult> ExecuteScalarFunctionAsync<TResult>(string functionName, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException("Function name cannot be empty.", nameof(functionName));

            // Validate against whitelist
            ValidateStoredProcedureName(functionName);

            try
            {
                var paramPlaceholders = string.Join(", ", parameters.Select((p, i) => $"@p{i}"));
                var sql = $"SELECT {functionName}({paramPlaceholders})";
                
                var result = await Db.Database.SqlQueryRaw<TResult>(sql, parameters).FirstOrDefaultAsync();
                return result!;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing scalar function {Function} for {Entity}", functionName, typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Executes a table-valued function and returns the results as entities.
        /// </summary>
        /// <typeparam name="TResult">The type of the result entities.</typeparam>
        /// <param name="functionName">The function name.</param>
        /// <param name="parameters">Parameters for the function.</param>
        /// <returns>A list of entities returned by the function.</returns>
        /// <exception cref="ArgumentException">Thrown when function name is empty.</exception>
        public async Task<IEnumerable<TResult>> ExecuteTableValuedFunctionAsync<TResult>(string functionName, params object[] parameters) where TResult : class
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException("Function name cannot be empty.", nameof(functionName));

            // Validate against whitelist
            ValidateStoredProcedureName(functionName);

            try
            {
                var paramPlaceholders = string.Join(", ", parameters.Select((p, i) => $"@p{i}"));
                var sql = $"SELECT * FROM {functionName}({paramPlaceholders})";
                
                return await Db.Set<TResult>().FromSqlRaw(sql, parameters).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing table-valued function {Function} for {Entity}", functionName, typeof(T).Name);
                throw;
            }
        }
        #endregion
    }
}
