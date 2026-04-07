namespace Repo.Repository.Security
{
    /// <summary>
    /// Configuration options for stored procedure and function whitelist validation.
    /// </summary>
    public class StoredProcedureOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether whitelist validation is required.
        /// When set to false (default), all stored procedures and functions are allowed for backward compatibility.
        /// When set to true, only whitelisted procedures and functions are allowed.
        /// </summary>
        public bool RequireWhitelist { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of whitelisted stored procedure and function names.
        /// Names are case-insensitive.
        /// </summary>
        public List<string> WhitelistedProcedures { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether to throw a SecurityException when validation fails.
        /// When set to true, a SecurityException is thrown for non-whitelisted procedures.
        /// When set to false, the validation passes without exception (for backward compatibility).
        /// Default is true when RequireWhitelist is true.
        /// </summary>
        public bool ThrowOnValidationFailure { get; set; } = true;

        /// <summary>
        /// Adds a stored procedure or function name to the whitelist.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <returns>The current options instance for method chaining.</returns>
        public StoredProcedureOptions AddWhitelistedProcedure(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !WhitelistedProcedures.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                WhitelistedProcedures.Add(name);
            }
            return this;
        }

        /// <summary>
        /// Adds multiple stored procedure or function names to the whitelist.
        /// </summary>
        /// <param name="names">The names to add.</param>
        /// <returns>The current options instance for method chaining.</returns>
        public StoredProcedureOptions AddWhitelistedProcedures(params string[] names)
        {
            foreach (var name in names)
            {
                AddWhitelistedProcedure(name);
            }
            return this;
        }
    }
}
