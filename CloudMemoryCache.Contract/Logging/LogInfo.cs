using System;

namespace CloudMemoryCache.Contract.Logging
{
    public class LogInfo
    {
        public int Id { get; set; }
        public string HostName { get; set; }
        public Guid CacheInstanceId { get; set; }
        public string Level { get; set; }
        public string Operation { get; set; }
        public string Message { get; set; }
        public string RelatedInfo { get; set; }
        public string MachineName { get; set; }
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public string CommandLine { get; set; }
        public string UserName { get; set; }
        public string Memory { get; set; }
        public DateTimeOffset EntryTime { get; set; }
    }
}
