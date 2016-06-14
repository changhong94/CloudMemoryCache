using System;
using System.Threading.Tasks;
using CloudMemoryCache.Contract;
using CloudMemoryCache.Contract.AccountManagement;
using CloudMemoryCache.Utils;
using RestSharp;

namespace CloudMemoryCache.Client.AccountManagement
{
    public class AccountManagerClient : ClientBase, IAccountManagerClient
    {
        public AccountManagerClient(IConfigurationUtil configurationUtil)
            : base(configurationUtil)
        {

        }

        public async Task<AccountRegisterResponse> RegisterAccountAsync(string connectionString, string email)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/Register", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { connectionString, email });
            var response = await client.ExecutePostTaskAsync<AccountRegisterResponse>(request);
            return response.Data;
        }

        public AccountRegisterResponse RegisterAccount(string connectionString, string email)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/Register", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { connectionString, email });
            var response = client.Execute<AccountRegisterResponse>(request);
            return response.Data;
        }

        public async Task<OperationResponse<bool>> ChangeEmailAsync(string hostName, Guid? ownerKey, string newEmail)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/ChangeEmail", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("ownerKey", ownerKey)
                   .AddParameter("newEmail", newEmail);
            var response = await client.ExecuteGetTaskAsync<OperationResponse<bool>>(request);
            return response.Data;
        }

        public OperationResponse<bool> ChangeEmail(string hostName, Guid? ownerKey, string newEmail)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/ChangeEmail", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("ownerKey", ownerKey)
                   .AddParameter("newEmail", newEmail);
            var response = client.Execute<OperationResponse<bool>>(request);
            return response.Data;
        }

        public async Task<OperationResponse<bool>> UpdateConnectionStringAsync(string hostName, string connectionString)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/UpdateConnectionString", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { hostName, connectionString });
            var response = await client.ExecutePostTaskAsync<OperationResponse<bool>>(request);
            return response.Data;
        }

        public OperationResponse<bool> UpdateConnectionString(string hostName, string connectionString)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/UpdateConnectionString", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { hostName, connectionString });
            var response = client.Execute<OperationResponse<bool>>(request);
            return response.Data;
        }

        public async Task<OperationResponse<bool>> RequestToSendOwnerKeyAsync(string hostName, string email)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/RequestToSendOwnerKey", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("email", email);
            var response = await client.ExecuteGetTaskAsync<OperationResponse<bool>>(request);
            return response.Data;
        }

        public OperationResponse<bool> RequestToSendOwnerKey(string hostName, string email)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/RequestToSendOwnerKey", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("email", email);
            var response = client.Execute<OperationResponse<bool>>(request);
            return response.Data;
        }

        public async Task<DataKeyResponse> GetDataKeyAsync(string hostName, Guid? ownerKey)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/GetDataKey", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("ownerKey", ownerKey);
            var response = await client.ExecuteGetTaskAsync<DataKeyResponse>(request);
            return response.Data;
        }

        public DataKeyResponse GetDataKey(string hostName, Guid? ownerKey)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/GetDataKey", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("ownerKey", ownerKey);
            var response = client.Execute<DataKeyResponse>(request);
            return response.Data;
        }

        public async Task<OperationResponse<bool>> ValidateDataKeyAsync(string hostName, Guid? dataKey)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/ValidateDataKey", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("dataKey", dataKey);
            var response = await client.ExecuteGetTaskAsync<OperationResponse<bool>>(request);
            return response.Data;
        }

        public OperationResponse<bool> ValidateDataKey(string hostName, Guid? dataKey)
        {
            var client = new RestClient(CloudMemoryCacheWebUrl);
            var request = new RestRequest("API/Account/ValidateDataKey", Method.GET);
            request.AddParameter("hostName", hostName)
                   .AddParameter("dataKey", dataKey);
            var response = client.Execute<OperationResponse<bool>>(request);
            return response.Data;
        }
    }
}