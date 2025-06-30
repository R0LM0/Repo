using Microsoft.AspNetCore.Mvc;
using Repo.Repository.Base;
using Repo.Repository.Models;
using Repo.Repository.UnitOfWork;
using Repo.Repository.Services;
using AutoMapper;
using FluentValidation;

namespace EjemploAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IRepo<User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ValidationService _validationService;
        private readonly IMapper _mapper;

        public UsersController(
            IRepo<User> userRepo,
            IUnitOfWork unitOfWork,
            ValidationService validationService,
            IMapper mapper)
        {
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtiene todos los usuarios
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            try
            {
                var users = await _userRepo.GetAllAsync();
                var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un usuario por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _userRepo.GetById(id);
                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                var userDto = _mapper.Map<UserDto>(user);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene usuarios con paginación
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<UserDto>>> GetUsersPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isAscending = true)
        {
            try
            {
                var request = new PagedRequest
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    SearchTerm = search,
                    SortBy = sortBy,
                    IsAscending = isAscending,
                    Filters = new Dictionary<string, object>
                    {
                        { "IsActive", true }
                    }
                };

                var result = await _userRepo.GetPagedAsync(request);
                var userDtos = _mapper.Map<IEnumerable<UserDto>>(result.Items);

                var pagedResult = new PagedResult<UserDto>
                {
                    Items = userDtos,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount,
                    TotalPages = result.TotalPages,
                    HasPreviousPage = result.HasPreviousPage,
                    HasNextPage = result.HasNextPage
                };

                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene usuarios activos
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetActiveUsers()
        {
            try
            {
                var users = await _userRepo.GetAllAsync();
                var activeUsers = users.Where(u => u.IsActive);
                var userDtos = _mapper.Map<IEnumerable<UserDto>>(activeUsers);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(UserDto userDto)
        {
            try
            {
                // Validar DTO
                var validator = new UserValidator();
                var user = _mapper.Map<User>(userDto);
                var isValid = await _validationService.IsValidAsync(user, validator);

                if (!isValid)
                {
                    var result = await _validationService.ValidateAsync(user, validator);
                    return BadRequest(result.Errors);
                }

                // Verificar si el email ya existe
                var existingUser = await _userRepo.GetAllAsync();
                if (existingUser.Any(u => u.Email == user.Email))
                    return BadRequest("El email ya está registrado");

                // Crear usuario
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;
                var createdUser = await _userRepo.Insert(user);
                await _unitOfWork.SaveChangesAsync();

                var createdUserDto = _mapper.Map<UserDto>(createdUser);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUserDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserDto userDto)
        {
            try
            {
                if (id != userDto.Id)
                    return BadRequest("El ID de la URL no coincide con el ID del usuario");

                // Verificar si el usuario existe
                var existingUser = await _userRepo.GetById(id);
                if (existingUser == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                // Validar DTO
                var validator = new UserValidator();
                var user = _mapper.Map<User>(userDto);
                var isValid = await _validationService.IsValidAsync(user, validator);

                if (!isValid)
                {
                    var result = await _validationService.ValidateAsync(user, validator);
                    return BadRequest(result.Errors);
                }

                // Verificar si el email ya existe en otro usuario
                var allUsers = await _userRepo.GetAllAsync();
                if (allUsers.Any(u => u.Email == user.Email && u.Id != id))
                    return BadRequest("El email ya está registrado por otro usuario");

                // Actualizar usuario
                user.UpdatedAt = DateTime.UtcNow;
                user.CreatedAt = existingUser.CreatedAt; // Mantener fecha de creación original
                await _userRepo.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un usuario
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _userRepo.GetById(id);
                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                await _userRepo.Delete(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Desactiva un usuario (soft delete)
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _userRepo.GetById(id);
                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepo.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Activa un usuario
        /// </summary>
        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                var user = await _userRepo.GetById(id);
                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepo.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca usuarios por término de búsqueda
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest("El término de búsqueda no puede estar vacío");

                var allUsers = await _userRepo.GetAllAsync();
                var filteredUsers = allUsers.Where(u =>
                    u.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Username.Contains(term, StringComparison.OrdinalIgnoreCase));

                var userDtos = _mapper.Map<IEnumerable<UserDto>>(filteredUsers);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}