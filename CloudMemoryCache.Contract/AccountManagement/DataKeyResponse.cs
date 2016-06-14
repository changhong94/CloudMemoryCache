using System;

namespace CloudMemoryCache.Contract.AccountManagement
{
    public class DataKeyResponse
    {
        public OperationResponse<bool> OperationResponse { get; set; }
        public Guid? DataKey { get; set; }
    }
}
