using System;

namespace CloudMemoryCache
{
    public class CacheItemConfig
    {
        public CacheItemConfig()
        {
            AbsoluteExpiration = DateTimeOffset.MaxValue;
            NeedSyncToMemory = true;
            CacheItemInfo = new CacheItemInfo();
        }

        public DateTimeOffset AbsoluteExpiration { get; set; }
        public bool NeedSyncToMemory { get; set; }
        public CacheItemInfo CacheItemInfo { get; private set; }
        public string Category { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
