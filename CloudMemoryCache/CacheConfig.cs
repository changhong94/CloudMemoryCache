using System;
using Newtonsoft.Json;

namespace CloudMemoryCache
{
    public class CacheConfig
    {
        public CacheConfig()
        {
            LocalCacheMemoryLimitInMB = 1024;
            PollingIntervalInSeconds = 60;
            NeedSyncToMemory = true;
            PrioritizedSyncToLocalKeys =new string[0];
        }

        public bool WaitForClusterReady { get; set; }
        public long LocalCacheMemoryLimitInMB { get; set; }
        public long SizeLimitForSyncToMemoryInBytes { get; set; }
        public long SizeLimitForSetCacheInBytes { get; set; }
        public int PollingIntervalInSeconds { get; set; }
        public bool NeedSyncToMemory { get; set; }
        public string[] PrioritizedSyncToLocalKeys { get; set; }
        public string[] FocusedCategories { get; set; }
        public string AdditionalInfo { get; set; }
        public bool DisableCacheUsage { get; set; }
        public bool DisableMemoryUsage { get; set; }
        public bool NotGetCacheValueFromCloud { get; set; }

        [JsonIgnore]
        public Action<string> OnSyncAzureToLocalCompleted { get; set; }
        [JsonIgnore]
        public Action<CacheChange> OnCacheChanged { get; set; }
        [JsonIgnore]
        public Action<CacheGetResult> OnCacheGot { get; set; }
    }
}
