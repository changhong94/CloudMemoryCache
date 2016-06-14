using System;

namespace CloudMemoryCache
{
    public class CacheItemInfo
    {
        public bool IsInMemory { get; set; }
        public DateTimeOffset? InMemoryTime { get; set; }
        public DateTimeOffset SetTime { get; set; }
        public string SetMachineName { get; set; }
        public string SetUserName { get; set; }
        public int Length { get; set; }
        public bool IsCompressed { get; set; }
        public string ValueTypeName { get; set; }
    }
}
