using System;

namespace CloudMemoryCache
{
    public class CacheChange
    {
        public CacheChangeType CacheChangeType { get; set; }
        public Guid PublishClientId { get; set; }
        public string CacheKey { get; set; }
        public CacheItemConfig CacheItemConfig { get; set; }
    }
}
