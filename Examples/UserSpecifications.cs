using Microsoft.EntityFrameworkCore;
using Repo.Repository.Specifications;
using System.Linq.Expressions;

// Ejemplo de especificaciones y entidades para referencia

// Clase de ejemplo User (para las especificaciones)
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserProfile? Profile { get; set; }
}

public class UserProfile
{
    public int Id { get; set; }
    public string? Bio { get; set; }
    public string? Avatar { get; set; }
}

// Ejemplo de especificación para usuarios activos
public class ActiveUsersSpecification : BaseSpecification<User>
{
    public ActiveUsersSpecification()
    {
        AddCriteria(user => user.IsActive);
        AddOrderBy(user => user.CreatedAt);
    }
}

// Ejemplo de especificación para usuarios por rol
public class UsersByRoleSpecification : BaseSpecification<User>
{
    public UsersByRoleSpecification(string role)
    {
        AddCriteria(user => user.Role == role);
        AddInclude(user => user.Profile);
        AddOrderBy(user => user.Name);
    }
}

// Ejemplo de especificación para búsqueda de usuarios
public class UserSearchSpecification : BaseSpecification<User>
{
    public UserSearchSpecification(string searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        AddCriteria(user => user.Name.Contains(searchTerm) ||
                           user.Email.Contains(searchTerm) ||
                           user.Username.Contains(searchTerm));
        AddOrderBy(user => user.Name);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

// Ejemplo de especificación para usuarios con paginación
public class UsersWithPaginationSpecification : BaseSpecification<User>
{
    public UsersWithPaginationSpecification(int pageNumber, int pageSize, string? sortBy = null, bool isAscending = true)
    {
        if (!string.IsNullOrEmpty(sortBy))
        {
            if (isAscending)
                AddOrderBy(user => EF.Property<object?>(user, sortBy));
            else
                AddOrderByDescending(user => EF.Property<object?>(user, sortBy));
        }
        else
        {
            AddOrderBy(user => user.CreatedAt);
        }

        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}