using Microsoft.Extensions.Options;

namespace Repo.Repository.Security
{
    /// <summary>
    /// Default implementation of <see cref="IStoredProcedureWhitelist"/>.
    /// Validates stored procedure and function names against a configured whitelist.
    /// </summary>
    public class DefaultStoredProcedureWhitelist : IStoredProcedureWhitelist
    {
        private readonly StoredProcedureOptions _options;
        private readonly HashSet<string> _whitelist;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultStoredProcedureWhitelist"/> class.
        /// </summary>
        /// <param name="options">The options containing the whitelist configuration.</param>
        public DefaultStoredProcedureWhitelist(IOptions<StoredProcedureOptions> options)
        {
            _options = options?.Value ?? new StoredProcedureOptions();
            
            // Initialize whitelist with case-insensitive comparison
            _whitelist = new HashSet<string>(
                _options.WhitelistedProcedures ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates if the specified stored procedure or function name is allowed.
        /// When whitelist is disabled (RequireWhitelist = false), all names are allowed.
        /// When whitelist is empty and disabled, all names are allowed for backward compatibility.
        /// </summary>
        /// <param name="name">The name of the stored procedure or function to validate.</param>
        /// <returns>True if the name is whitelisted or if whitelist validation is disabled; otherwise, false.</returns>
        public bool IsAllowed(string name)
        {
            // If whitelist validation is disabled, allow all (backward compatibility)
            if (!_options.RequireWhitelist)
            {
                return true;
            }

            // If name is null or empty, reject it
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            // Extract the actual procedure name from SQL (handles "EXEC sp_name" format)
            var actualName = ExtractProcedureName(name);

            return _whitelist.Contains(actualName);
        }

        /// <summary>
        /// Gets all whitelisted stored procedure and function names.
        /// </summary>
        /// <returns>An enumerable collection of whitelisted names.</returns>
        public IEnumerable<string> GetWhitelistedNames()
        {
            return _whitelist.ToList().AsReadOnly();
        }

        /// <summary>
        /// Extracts the actual procedure name from a SQL command string.
        /// Handles formats like "EXEC sp_name @param" or "sp_name".
        /// </summary>
        /// <param name="sql">The SQL command string.</param>
        /// <returns>The extracted procedure name.</returns>
        private static string ExtractProcedureName(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return string.Empty;

            // Trim and normalize
            var normalized = sql.Trim();

            // Handle EXEC/EXECUTE prefix
            if (normalized.StartsWith("EXEC ", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("EXECUTE ", StringComparison.OrdinalIgnoreCase))
            {
                // Remove EXEC/EXECUTE prefix
                var afterExec = normalized.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (afterExec.Length > 1)
                {
                    normalized = afterExec[1].Trim();
                }
            }

            // Handle SELECT * FROM functionName(...) format for TVFs
            if (normalized.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
            {
                // Try to extract function name from SELECT * FROM functionName(...)
                var fromIndex = normalized.IndexOf(" FROM ", StringComparison.OrdinalIgnoreCase);
                if (fromIndex > 0)
                {
                    var afterFrom = normalized.Substring(fromIndex + 6).Trim();
                    // Extract just the function name (before any parentheses or spaces)
                    var endIndex = afterFrom.IndexOfAny(new[] { '(', ' ' });
                    if (endIndex > 0)
                    {
                        return afterFrom.Substring(0, endIndex).Trim();
                    }
                    return afterFrom;
                }
            }

            // Handle SELECT functionName(...) format for scalar functions
            if (normalized.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
            {
                var afterSelect = normalized.Substring(7).Trim();
                var endIndex = afterSelect.IndexOf('(');
                if (endIndex > 0)
                {
                    return afterSelect.Substring(0, endIndex).Trim();
                }
            }

            // Extract the first token (procedure name) before any parameters or parentheses
            var firstTokenEnd = normalized.IndexOfAny(new[] { ' ', '(', '@' });
            if (firstTokenEnd > 0)
            {
                return normalized.Substring(0, firstTokenEnd).Trim();
            }

            return normalized;
        }
    }
}
