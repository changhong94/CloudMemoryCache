using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace CloudMemoryCache.Common.TransientFaultHandling
{
    public class AllExceptionTransientErrorDetectionStrategy: ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            return true;
        }
    }
}
