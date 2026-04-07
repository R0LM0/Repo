namespace Repo.Repository.Security
{
    /// <summary>
    /// Defines the contract for validating stored procedure and function names against a whitelist.
    /// </summary>
    public interface IStoredProcedureWhitelist
    {
        /// <summary>
        /// Validates if the specified stored procedure or function name is allowed.
        /// </summary>
        /// <param name="name">The name of the stored procedure or function to validate.</param>
        /// <returns>True if the name is whitelisted; otherwise, false.</returns>
        bool IsAllowed(string name);

        /// <summary>
        /// Gets all whitelisted stored procedure and function names.
        /// </summary>
        /// <returns>An enumerable collection of whitelisted names.</returns>
        IEnumerable<string> GetWhitelistedNames();
    }
}
