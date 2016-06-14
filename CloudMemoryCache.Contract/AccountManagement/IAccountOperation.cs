using System;

namespace CloudMemoryCache.Contract.AccountManagement
{
    public interface IAccountOperation
    {
        AccountRegisterResponse RegisterAccount(string connectionString, string email);
        OperationResponse<bool> ChangeEmail(string hostName, Guid? ownerKey, string newEmail);
        OperationResponse<bool> UpdateConnectionString(string hostName, string connectionString);
        OperationResponse<bool> RequestToSendOwnerKey(string hostName, string email);
        DataKeyResponse GetDataKey(string hostName, Guid? ownerKey);
        OperationResponse<bool> ValidateDataKey(string hostName, Guid? dataKey);
    }
}
