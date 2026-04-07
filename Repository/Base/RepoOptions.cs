using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Configuration options for the repository.
    /// </summary>
    public class RepoOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether compiled queries are enabled.
        /// Default is false for backward compatibility.
        /// </summary>
        /// <remarks>
        /// Compiled queries provide 20-30% performance improvement for frequently used operations
        /// like GetById, GetAllAsync, and CountAsync. However, they use additional memory.
        /// Enable this only when you have identified performance bottlenecks.
        /// </remarks>
        public bool EnableCompiledQueries { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to log when compiled queries are used.
        /// </summary>
        public bool LogCompiledQueryUsage { get; set; } = false;
    }
}
