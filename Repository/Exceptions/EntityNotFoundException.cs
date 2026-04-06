namespace Repo.Repository.Exceptions
{
    /// <summary>
    /// Exception thrown when an entity is not found in the repository.
    /// </summary>
    public class EntityNotFoundException : RepositoryException
    {
        public EntityNotFoundException(string message) : base(message) { }
        public EntityNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}