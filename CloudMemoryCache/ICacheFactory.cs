using System;
using System.Threading.Tasks;

namespace CloudMemoryCache
{
    public interface ICacheFactory
    {
        ICache CreateInstance(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null);
        Task<ICache> CreateInstanceAsync(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null);
    }
}
