using System;
using System.Threading.Tasks;

namespace CloudMemoryCache
{
    public class CacheFactory : ICacheFactory
    {
        public ICache CreateInstance(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null)
        {
            var cache = new Cache();
            cache.Initialize(connectionString, dataKey, cacheConfig);
            return cache;
        }

        public async Task<ICache> CreateInstanceAsync(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null)
        {
            var cache = new Cache();
            await cache.InitializeAsync(connectionString, dataKey, cacheConfig);
            return cache;
        }
    }
}