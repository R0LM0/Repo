using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Repo.Repository.Services
{
    public class MappingService
    {
        private readonly IMapper _mapper;
        private readonly ILogger<MappingService> _logger;

        public MappingService(IMapper mapper, ILogger<MappingService> logger)
        {
            _mapper = mapper;
            _logger = logger;
        }

        public TDestination Map<TDestination>(object source)
        {
            try
            {
                return _mapper.Map<TDestination>(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping from {SourceType} to {DestinationType}",
                    source.GetType().Name, typeof(TDestination).Name);
                throw;
            }
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            try
            {
                return _mapper.Map<TSource, TDestination>(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping from {SourceType} to {DestinationType}",
                    typeof(TSource).Name, typeof(TDestination).Name);
                throw;
            }
        }

        public IEnumerable<TDestination> MapCollection<TSource, TDestination>(IEnumerable<TSource> source)
        {
            try
            {
                return _mapper.Map<IEnumerable<TSource>, IEnumerable<TDestination>>(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping collection from {SourceType} to {DestinationType}",
                    typeof(TSource).Name, typeof(TDestination).Name);
                throw;
            }
        }

        public void Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            try
            {
                _mapper.Map(source, destination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping from {SourceType} to {DestinationType}",
                    typeof(TSource).Name, typeof(TDestination).Name);
                throw;
            }
        }

        public async Task<TDestination> MapAsync<TDestination>(object source)
        {
            return await Task.FromResult(Map<TDestination>(source));
        }

        public async Task<TDestination> MapAsync<TSource, TDestination>(TSource source)
        {
            return await Task.FromResult(Map<TSource, TDestination>(source));
        }

        public async Task<IEnumerable<TDestination>> MapCollectionAsync<TSource, TDestination>(IEnumerable<TSource> source)
        {
            return await Task.FromResult(MapCollection<TSource, TDestination>(source));
        }
    }
}