namespace Repo.Repository.Exceptions
{
    /// <summary>
    /// Exception thrown when a security validation fails, such as when a stored procedure
    /// or function is not in the whitelist.
    /// </summary>
    public class SecurityException : RepositoryException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public SecurityException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Creates a SecurityException for a non-whitelisted stored procedure or function.
        /// </summary>
        /// <param name="procedureName">The name of the procedure that was not whitelisted.</param>
        /// <returns>A new SecurityException with a descriptive message.</returns>
        public static SecurityException ProcedureNotWhitelisted(string procedureName)
        {
            return new SecurityException(
                $"Security validation failed: The stored procedure or function '{procedureName}' is not in the whitelist. " +
                "Access denied. To allow this procedure, add it to the StoredProcedureOptions.WhitelistedProcedures collection.");
        }
    }
}
