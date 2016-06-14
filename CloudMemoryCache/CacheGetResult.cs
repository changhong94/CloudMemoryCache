using System;

namespace CloudMemoryCache
{
    public class CacheGetResult
    {
        public CacheGetResult()
        {
            EntryTime = DateTimeOffset.Now;
        }

        public string Key { get; set; }
        public string ValueTypeName { get; set; }
        public bool IsInMemory { get; set; }
        public bool IsValueNull { get; set; }
        public bool IsCompressed { get; set; }
        public long CacheElapsedMilliseconds { get; set; }
        public long RetrievingElapsedMilliseconds { get; set; }
        public long TotalElapsedMilliseconds { get; set; }
        public DateTimeOffset EntryTime { get; set; }
    }
}
