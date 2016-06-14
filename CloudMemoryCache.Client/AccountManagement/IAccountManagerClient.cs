using System;
using System.Threading.Tasks;
using CloudMemoryCache.Contract;
using CloudMemoryCache.Contract.AccountManagement;

namespace CloudMemoryCache.Client.AccountManagement
{
    public interface IAccountManagerClient : IAccountOperation
    {
        Task<AccountRegisterResponse> RegisterAccountAsync(string connectionString, string email);
        Task<OperationResponse<bool>> ChangeEmailAsync(string hostName, Guid? ownerKey, string newEmail);
        Task<OperationResponse<bool>> UpdateConnectionStringAsync(string hostName, string connectionString); 
        Task<OperationResponse<bool>> RequestToSendOwnerKeyAsync(string hostName, string email);
        Task<DataKeyResponse> GetDataKeyAsync(string hostName, Guid? ownerKey);
        Task<OperationResponse<bool>> ValidateDataKeyAsync(string hostName, Guid? dataKey);
    }
}
