using System.Net;
using System.Threading.Tasks;
using CloudMemoryCache.Common.TransientFaultHandling;
using CloudMemoryCache.Contract;
using StackExchange.Redis;

namespace CloudMemoryCache
{
    public static class AzureRedisCacheUtil
    {
        public static OperationResponse<bool> TestConnectionString(string connectionString)
        {
            string hostName = null;
            var result = TransientFaultHandlingUtil.SafeExecute(() =>
            {
                var connection = ConnectionMultiplexer.Connect(connectionString);
                hostName = (connection.GetEndPoints()[0] as DnsEndPoint).Host;
            }, retryCount: 1);
            if (result.IsSuccessful)
            {
                result.Messages.Add(hostName);
            }
            else
            {
                result.Messages.Clear();
                result.Messages.Add("The ConnectionString failed to connect to Azure Redis Cache.");
            }
            return result;
        }

        public async static Task<OperationResponse<bool>> TestConnectionStringAsync(string connectionString)
        {
            string hostName = null;
            var result = await TransientFaultHandlingUtil.SafeExecuteAsync(async () =>
            {
                var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
                hostName = (connection.GetEndPoints()[0] as DnsEndPoint).Host;
            }, retryCount: 1);
            if (result.IsSuccessful)
            {
                result.Messages.Add(hostName);
            }
            else
            {
                result.Messages.Clear();
                result.Messages.Add("The ConnectionString failed to connect to Azure Redis Cache.");
            }
            return result;
        }
    }
}
