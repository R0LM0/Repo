using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Repo.Repository.Services
{
    public class ValidationService
    {
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateAsync<T>(T entity, IValidator<T> validator)
        {
            try
            {
                var result = await validator.ValidateAsync(entity);

                if (!result.IsValid)
                {
                    _logger.LogWarning("Validation failed for {EntityType}: {Errors}",
                        typeof(T).Name, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during validation for {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public ValidationResult Validate<T>(T entity, IValidator<T> validator)
        {
            try
            {
                var result = validator.Validate(entity);

                if (!result.IsValid)
                {
                    _logger.LogWarning("Validation failed for {EntityType}: {Errors}",
                        typeof(T).Name, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during validation for {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<bool> IsValidAsync<T>(T entity, IValidator<T> validator)
        {
            var result = await ValidateAsync(entity, validator);
            return result.IsValid;
        }

        public bool IsValid<T>(T entity, IValidator<T> validator)
        {
            var result = Validate(entity, validator);
            return result.IsValid;
        }
    }
}