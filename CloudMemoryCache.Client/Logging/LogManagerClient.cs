using System;
using System.Threading.Tasks;
using CloudMemoryCache.Contract;
using CloudMemoryCache.Contract.Logging;
using CloudMemoryCache.Utils;
using Newtonsoft.Json;
using RestSharp;

namespace CloudMemoryCache.Client.Logging
{
    public class LogManagerClient : ClientBase, ILogManagerClient
    {
        public LogManagerClient(IConfigurationUtil configurationUtil)
            : base(configurationUtil)
        {

        }

        public OperationResponse<bool> Log(LogItem logItem)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("Log", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddObject(logItem);
            var response = client.Execute<OperationResponse<bool>>(request);
            return response.Data;
        }

        public async Task<OperationResponse<bool>> LogAsync(LogItem logItem)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("Log", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddObject(logItem);
            var response = await client.ExecutePostTaskAsync<OperationResponse<bool>>(request);
            return response.Data;
        }
    }
}