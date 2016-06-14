namespace CloudMemoryCache.Common.Logging
{
    public interface ILogger
    {
        string HostName { get; set; }
        void Log(string level, string operation, string message, object relatedInfo);
    }
}
