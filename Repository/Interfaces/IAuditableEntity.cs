namespace Repo.Repository.Interfaces
{
    public interface IAuditableEntity
    {
        DateTime? CreatedAt { get; set; }
        string? CreatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
        bool? IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }

    public interface ISoftDelete
    {
        bool? IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }
}