using System.Collections.Concurrent;
using System.Threading.Tasks;
using CloudMemoryCache.Client.Logging;
using CloudMemoryCache.Contract.Logging;
using CloudMemoryCache.Utils;

namespace CloudMemoryCache.Common.Logging
{
    public static class LogItemContainer
    {
        private static readonly ConcurrentQueue<LogItem> LogItems = new ConcurrentQueue<LogItem>();

        public static void Add(LogItem logItem)
        {
            LogItems.Enqueue(logItem);

            Task.Factory.StartNew(HandleLogs);
        }

        private static object _lockObject = new object();
        private static bool _isHandling = false;
        private static void HandleLogs()
        {
            if (_isHandling)
            {
                return;
            }
            if (!_isHandling)
            {
                lock (_lockObject)
                {
                    if (!_isHandling)
                    {
                        _isHandling = true;
                    }
                }
            }
            try
            {
                var logManagerClient = new LogManagerClient(new ConfigurationUtil());
                LogItem logItem;
                while (LogItems.TryDequeue(out logItem))
                {
                    var logResponse = logManagerClient.Log(logItem);
                    if (logResponse == null || !logResponse.IsSuccessful)
                    {
                        logManagerClient.Log(logItem);
                    }
                }
            }
            finally
            {
                _isHandling = false;
            }
        }
    }
}