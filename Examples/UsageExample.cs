using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using Repo.Repository.Models;
using Repo.Repository.Services;
using Repo.Repository.Specifications;
using Repo.Repository.Specifications.Examples;
using Repo.Repository.UnitOfWork;
using FluentValidation;

namespace Repo.Examples
{
    public class UsageExample
    {
        private readonly IRepo<User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ValidationService _validationService;
        private readonly MappingService _mappingService;

        public UsageExample(
            IRepo<User> userRepo,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ValidationService validationService,
            MappingService mappingService)
        {
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _validationService = validationService;
            _mappingService = mappingService;
        }

        public async Task RunExamplesAsync()
        {
            Console.WriteLine("🚀 Ejemplos de uso de la Librería de Repositorio Avanzada\n");

            // 1. Operaciones CRUD básicas
            await BasicCrudOperationsAsync();

            // 2. Paginación y filtrado
            await PaginationAndFilteringAsync();

            // 3. Especificaciones
            await SpecificationsExampleAsync();

            // 4. Caché
            await CacheExampleAsync();

            // 5. Validación
            await ValidationExampleAsync();

            // 6. Mapeo
            await MappingExampleAsync();

            // 7. Soft Delete
            await SoftDeleteExampleAsync();

            // 8. Bulk Operations
            await BulkOperationsExampleAsync();

            // 9. Unit of Work
            await UnitOfWorkExampleAsync();

            // 10. Búsqueda avanzada
            await AdvancedSearchExampleAsync();
        }

        private async Task BasicCrudOperationsAsync()
        {
            Console.WriteLine("📝 1. Operaciones CRUD Básicas");

            // Crear usuario
            var user = new User
            {
                Name = "John Doe",
                Email = "john@example.com",
                Username = "johndoe",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepo.Insert(user);
            Console.WriteLine($"✅ Usuario creado: {createdUser.Name}");

            // Obtener por ID
            var retrievedUser = await _userRepo.GetById(createdUser.Id);
            Console.WriteLine($"📖 Usuario obtenido: {retrievedUser.Name}");

            // Actualizar
            retrievedUser.Name = "John Updated";
            var updatedUser = await _userRepo.UpdateAsync(retrievedUser);
            Console.WriteLine($"✏️ Usuario actualizado: {updatedUser.Name}");

            // Obtener todos
            var allUsers = await _userRepo.GetAllAsync();
            Console.WriteLine($"📋 Total de usuarios: {allUsers.Count()}\n");
        }

        private async Task PaginationAndFilteringAsync()
        {
            Console.WriteLine("📄 2. Paginación y Filtrado");

            var request = new PagedRequest
            {
                PageNumber = 1,
                PageSize = 5,
                SortBy = "Name",
                IsAscending = true,
                SearchTerm = "john",
                Filters = new Dictionary<string, object>
                {
                    { "IsActive", true }
                }
            };

            var result = await _userRepo.GetPagedAsync(request);
            Console.WriteLine($"📊 Resultados paginados:");
            Console.WriteLine($"   Total: {result.TotalCount}");
            Console.WriteLine($"   Página: {result.PageNumber} de {result.TotalPages}");
            Console.WriteLine($"   Elementos por página: {result.PageSize}");
            Console.WriteLine($"   Tiene página anterior: {result.HasPreviousPage}");
            Console.WriteLine($"   Tiene página siguiente: {result.HasNextPage}\n");
        }

        private async Task SpecificationsExampleAsync()
        {
            Console.WriteLine("🎯 3. Especificaciones (Specification Pattern)");

            // Especificación para usuarios activos
            var activeUsersSpec = new ActiveUsersSpecification();
            var activeUsers = await _userRepo.GetAllBySpecAsync(activeUsersSpec);
            Console.WriteLine($"✅ Usuarios activos: {activeUsers.Count()}");

            // Especificación para usuarios por rol
            var adminUsersSpec = new UsersByRoleSpecification("Admin");
            var adminUsers = await _userRepo.GetAllBySpecAsync(adminUsersSpec);
            Console.WriteLine($"👑 Usuarios admin: {adminUsers.Count()}");

            // Especificación con paginación
            var pagedSpec = new UsersWithPaginationSpecification(1, 3, "CreatedAt", false);
            var pagedUsers = await _userRepo.GetPagedBySpecAsync(pagedSpec, new PagedRequest { PageNumber = 1, PageSize = 3 });
            Console.WriteLine($"📄 Usuarios paginados: {pagedUsers.Items.Count()}\n");
        }

        private async Task CacheExampleAsync()
        {
            Console.WriteLine("💾 4. Caché con Redis");

            // Obtener usuario con caché
            var userWithCache = await _userRepo.GetByIdWithCacheAsync(1, TimeSpan.FromMinutes(30));
            Console.WriteLine($"🔄 Usuario con caché: {userWithCache?.Name}");

            // Obtener todos los usuarios con caché
            var allUsersWithCache = await _userRepo.GetAllWithCacheAsync(TimeSpan.FromHours(1));
            Console.WriteLine($"🔄 Todos los usuarios con caché: {allUsersWithCache.Count()}");

            // Invalidar caché
            await _userRepo.InvalidateCacheAsync("*");
            Console.WriteLine("🗑️ Caché invalidado\n");
        }

        private async Task ValidationExampleAsync()
        {
            Console.WriteLine("✅ 5. Validación con FluentValidation");

            var user = new User
            {
                Name = "", // Inválido - nombre vacío
                Email = "invalid-email", // Inválido - email mal formado
                Username = "validuser",
                Role = "User",
                IsActive = true
            };

            var validator = new UserValidator();
            var isValid = await _validationService.IsValidAsync(user, validator);
            Console.WriteLine($"🔍 Usuario válido: {isValid}");

            if (!isValid)
            {
                var result = await _validationService.ValidateAsync(user, validator);
                Console.WriteLine("❌ Errores de validación:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error.ErrorMessage}");
                }
            }
            Console.WriteLine();
        }

        private async Task MappingExampleAsync()
        {
            Console.WriteLine("🔄 6. Mapeo con AutoMapper");

            var user = new User
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Username = "johndoe",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Mapear a DTO
            var userDto = _mappingService.Map<User, UserDto>(user);
            Console.WriteLine($"🔄 Usuario mapeado a DTO: {userDto.Name}");

            // Mapear colección
            var users = new List<User> { user };
            var userDtos = _mappingService.MapCollection<User, UserDto>(users);
            Console.WriteLine($"🔄 Colección mapeada: {userDtos.Count()} elementos\n");
        }

        private async Task SoftDeleteExampleAsync()
        {
            Console.WriteLine("🗑️ 7. Soft Delete");

            // Soft delete
            await _userRepo.SoftDeleteAsync(1, "admin");
            Console.WriteLine("🗑️ Usuario marcado como eliminado");

            // Restaurar
            await _userRepo.RestoreAsync(1);
            Console.WriteLine("🔄 Usuario restaurado");

            // Obtener incluyendo eliminados
            var allUsersIncludingDeleted = await _userRepo.GetAllIncludingDeletedAsync();
            Console.WriteLine($"📋 Usuarios incluyendo eliminados: {allUsersIncludingDeleted.Count()}\n");
        }

        private async Task BulkOperationsExampleAsync()
        {
            Console.WriteLine("📦 8. Bulk Operations");

            var users = new List<User>
            {
                new User { Name = "User1", Email = "user1@example.com", Username = "user1", Role = "User", IsActive = true },
                new User { Name = "User2", Email = "user2@example.com", Username = "user2", Role = "User", IsActive = true },
                new User { Name = "User3", Email = "user3@example.com", Username = "user3", Role = "User", IsActive = true }
            };

            // Agregar múltiples usuarios
            var addedCount = await _userRepo.AddRangeAsync(users);
            Console.WriteLine($"➕ Usuarios agregados: {addedCount}");

            // Actualizar múltiples usuarios
            foreach (var user in users)
            {
                user.Name += " Updated";
            }
            var updatedCount = await _userRepo.UpdateRangeAsync(users);
            Console.WriteLine($"✏️ Usuarios actualizados: {updatedCount}");

            // Eliminar múltiples usuarios
            var deletedCount = await _userRepo.DeleteRangeAsync(users);
            Console.WriteLine($"🗑️ Usuarios eliminados: {deletedCount}\n");
        }

        private async Task UnitOfWorkExampleAsync()
        {
            Console.WriteLine("🏗️ 9. Unit of Work");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userRepo = _unitOfWork.Repository<User>();
                var profileRepo = _unitOfWork.Repository<UserProfile>();

                var user = new User
                {
                    Name = "Transaction User",
                    Email = "transaction@example.com",
                    Username = "transactionuser",
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var profile = new UserProfile
                {
                    Bio = "User created in transaction",
                    Avatar = "default.jpg"
                };

                await userRepo.Insert(user);
                await profileRepo.Insert(profile);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                Console.WriteLine("✅ Transacción completada exitosamente");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"❌ Transacción falló: {ex.Message}");
            }
            Console.WriteLine();
        }

        private async Task AdvancedSearchExampleAsync()
        {
            Console.WriteLine("🔍 10. Búsqueda Avanzada");

            // Búsqueda con filtros
            var activeUsers = await _userRepo.FindAsync(user => user.IsActive && user.Role == "User");
            Console.WriteLine($"🔍 Usuarios activos con rol User: {activeUsers.Count()}");

            // Búsqueda con includes
            var usersWithProfile = await _userRepo.FindAsync(
                user => user.IsActive,
                user => user.Profile
            );
            Console.WriteLine($"🔍 Usuarios con perfil: {usersWithProfile.Count()}");

            // Verificar existencia
            var exists = await _userRepo.AnyAsync(user => user.Email == "john@example.com");
            Console.WriteLine($"🔍 Usuario existe: {exists}");

            // Contar registros
            var count = await _userRepo.CountAsync(user => user.IsActive);
            Console.WriteLine($"🔍 Total de usuarios activos: {count}\n");
        }
    }

    // Clases de ejemplo
    public class User : ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserProfile? Profile { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    public class UserProfile
    {
        public int Id { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserValidator : AbstractValidator<User>
    {
        public UserValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3);
            RuleFor(x => x.Role).NotEmpty();
        }
    }

    // Especificaciones específicas para el ejemplo
    public class ActiveUsersSpecification : BaseSpecification<User>
    {
        public ActiveUsersSpecification()
        {
            AddCriteria(user => user.IsActive);
            AddOrderBy(user => user.CreatedAt);
        }
    }

    public class UsersByRoleSpecification : BaseSpecification<User>
    {
        public UsersByRoleSpecification(string role)
        {
            AddCriteria(user => user.Role == role);
            AddInclude(user => user.Profile);
            AddOrderBy(user => user.Name);
        }
    }

    public class UsersWithPaginationSpecification : BaseSpecification<User>
    {
        public UsersWithPaginationSpecification(int pageNumber, int pageSize, string? sortBy = null, bool isAscending = true)
        {
            if (!string.IsNullOrEmpty(sortBy))
            {
                if (isAscending)
                    AddOrderBy(user => EF.Property<object>(user, sortBy));
                else
                    AddOrderByDescending(user => EF.Property<object>(user, sortBy));
            }
            else
            {
                AddOrderBy(user => user.CreatedAt);
            }

            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }
    }
}