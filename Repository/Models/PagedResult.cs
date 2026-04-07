namespace Repo.Repository.Models
{
    /// <summary>
    /// Represents a paginated result set.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The paginated data items. This is the primary property.
        /// </summary>
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// [OBSOLETE] Use <see cref="Data"/> instead. This property is provided for backward compatibility.
        /// </summary>
        [Obsolete("Use Data property instead. This will be removed in a future version.", false)]
        public IEnumerable<T> Items
        {
            get => Data;
            set => Data = value?.ToList() ?? new List<T>();
        }

        public int TotalCount { get; set; }

        /// <summary>
        /// The current page number (1-based). This is the primary property.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// [OBSOLETE] Use <see cref="Page"/> instead. This property is provided for backward compatibility.
        /// </summary>
        [Obsolete("Use Page property instead. This will be removed in a future version.", false)]
        public int PageNumber
        {
            get => Page;
            set => Page = value;
        }

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The total number of pages calculated from TotalCount and PageSize.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
        /// </summary>
        public PagedResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class with the specified data.
        /// </summary>
        /// <param name="items">The paginated data items.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Data = items.ToList();
            TotalCount = totalCount;
            Page = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        }
    }

    /// <summary>
    /// Represents a request for paginated data.
    /// </summary>
    public class PagedRequest
    {
        /// <summary>
        /// The page number to retrieve (1-based). Default is 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The number of items per page. Default is 10.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// The property name to sort by. Null if no sorting is required.
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Whether to sort in ascending order. Default is true.
        /// </summary>
        public bool IsAscending { get; set; } = true;

        /// <summary>
        /// A search term to filter results. Null if no search is required.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Additional filters as key-value pairs. Null if no additional filters are required.
        /// </summary>
        public Dictionary<string, object>? Filters { get; set; }
    }
}