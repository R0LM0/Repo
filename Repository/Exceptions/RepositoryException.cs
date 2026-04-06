namespace Repo.Repository.Exceptions
{
    /// <summary>
    /// Base exception for all repository-related errors.
    /// </summary>
    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
    }
}