using System;

namespace CloudMemoryCache.Contract.AccountManagement
{
    public class AccountRegisterResponse
    {
        public OperationResponse<bool> OperationResponse { get; set; }
        public Guid? DataKey { get; set; }
        public Guid? OwnerKey { get; set; }
    }
}
