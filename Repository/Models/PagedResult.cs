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

        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        public PagedResult()
        {
        }

        public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Data = items.ToList();
            TotalCount = totalCount;
            Page = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        }
    }

    public class PagedRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsAscending { get; set; } = true;
        public string? SearchTerm { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
    }
}