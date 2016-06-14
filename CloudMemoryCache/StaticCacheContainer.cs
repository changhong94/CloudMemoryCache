using System;
using System.Dynamic;
using CloudMemoryCache.Contract;

namespace CloudMemoryCache
{
    public static class StaticCacheContainer
    {
        public static OperationResponse<bool> InitializeResult { get; private set; }

        static StaticCacheContainer()
        {
            InitializeResult = new OperationResponse<bool>
            {
                Value = false
            };
        }

        private static ICache _cache;
        public static ICache Cache
        {
            get
            {
                if (!InitializeResult.IsSuccessful)
                {
                    var message = InitializeResult.Message;
                    if (String.IsNullOrWhiteSpace(message))
                    {
                        throw new InvalidOperationException("The cache is not initialized. Please call StaticCacheContainer.Initialize first and make sure parameters are correct.");
                    }
                    else
                    {
                        throw new InvalidOperationException("The cache is not initialized. " + message);
                    }
                }
                return _cache;
            }
        }

        public static void Initialize(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null)
        {
            _cache = new CacheFactory().CreateInstance(connectionString, dataKey, cacheConfig);
            InitializeResult = _cache.InitializeResult;
        }

        public static void SetCacheOperator(ICache cache)
        {
            if (!cache.InitializeResult.IsSuccessful)
            {
                var message = cache.InitializeResult.Message;
                if (String.IsNullOrWhiteSpace(message))
                {
                    throw new InvalidOperationException("The cache is not initialized.");
                }
                else
                {
                    throw new InvalidOperationException("The cache is not initialized. " + message);
                }
            }
            _cache = cache;
            InitializeResult = _cache.InitializeResult;
        }
    }
}
