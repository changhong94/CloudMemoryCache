using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CloudMemoryCache.Common.TransientFaultHandling;
using CloudMemoryCache.Contract.Logging;
using Newtonsoft.Json;

namespace CloudMemoryCache.Common.Logging
{
    public class Logger : ILogger
    {
        public Logger(Guid cacheInstanceId)
        {
            CacheInstanceId = cacheInstanceId;
        }

        public string HostName { get; set; }
        public Guid CacheInstanceId { get; private set; }

        public void Log(string level, string operation, string message, object relatedInfo)
        {
            Task.Run(() =>
            {
                var currentProcess = Process.GetCurrentProcess();
                var logItem = new LogItem
                {
                    HostName = HostName,
                    CacheInstanceId = CacheInstanceId,
                    Level = level,
                    Operation = operation,
                    Message = message,
                    RelatedInfo = JsonConvert.SerializeObject(relatedInfo),
                    Memory = String.Format("PrivateMemorySize64:{0}, VirtualMemorySize64:{1}, PagedMemorySize64:{2}, PagedSystemMemorySize64:{3}, NonpagedSystemMemorySize64:{4}, PeakPagedMemorySize64:{5}, PeakVirtualMemorySize64:{6}",
                                            currentProcess.PrivateMemorySize64,
                                            currentProcess.VirtualMemorySize64,
                                            currentProcess.PagedMemorySize64,
                                            currentProcess.PagedSystemMemorySize64,
                                            currentProcess.NonpagedSystemMemorySize64,
                                            currentProcess.PeakPagedMemorySize64,
                                            currentProcess.PeakVirtualMemorySize64),
                    MachineName = Environment.MachineName,
                    ProcessName = currentProcess.ProcessName,
                    ProcessId = currentProcess.Id,
                    CommandLine = Environment.CommandLine,
                    UserName = Environment.UserName,
                    EntryTime = DateTimeOffset.Now
                };
                TransientFaultHandlingUtil.SafeExecute(() =>
                {
                    LogItemContainer.Add(logItem);
                }, needRetry: false, needLog: false);
            });
        }
    }
}