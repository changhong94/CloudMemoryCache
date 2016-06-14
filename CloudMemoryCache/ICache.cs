using System;
using System.Threading.Tasks;
using CloudMemoryCache.Contract;

namespace CloudMemoryCache
{
    public interface ICache : IDisposable
    {
        void Initialize(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null);
        Task InitializeAsync(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null);
        OperationResponse<bool> InitializeResult { get; }
        void Set<T>(string key, T value, CacheItemConfig cacheItemConfig = null);
        Task SetAsync<T>(string key, T value, CacheItemConfig cacheItemConfig = null);
        void Remove(string key);
        Task RemoveAsync(string key);
        T Get<T>(string key);
        Task<T> GetAsync<T>(string key);
        T GetOrRetrieve<T>(string key, Func<T> retriever, CacheItemConfig cacheItemConfig = null);
        Task<T> GetOrRetrieveAsync<T>(string key, Func<Task<T>> retriever, CacheItemConfig cacheItemConfig = null);
        string[] GetKeys();
        Task<string[]> GetKeysAsync();
        bool Contains(string key);
        Task<bool> ContainsAsync(string key);
        CacheItemConfig GetCacheItemConfig(string key);
        Task<CacheItemConfig> GetCacheItemConfigAsync(string key);
        int CalculateTotalLength();
        Task<int> CalculateTotalLengthAsync();

        bool SyncAzureToLocalCompleted { get; }
        string HostName { get; }
    }
}
