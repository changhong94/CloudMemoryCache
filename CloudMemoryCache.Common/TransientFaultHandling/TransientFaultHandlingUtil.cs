using System;
using System.Threading.Tasks;
using CloudMemoryCache.Common.Logging;
using CloudMemoryCache.Contract;

namespace CloudMemoryCache.Common.TransientFaultHandling
{
    public static class TransientFaultHandlingUtil
    {
        private static readonly ITransientFaultHandler TransientFaultHandler = new TransientFaultHandler(new Logger(Guid.Empty));

        public static void RetryAllException(Action action, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            TransientFaultHandler.RetryAllException(action, retryName, retryCount, firstFastRetry);
        }

        public static async Task RetryAllExceptionAsync(Func<Task> action, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            await TransientFaultHandler.RetryAllExceptionAsync(action, retryName, retryCount, firstFastRetry);
        }

        public static T RetryAllException<T>(Func<T> func, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            return TransientFaultHandler.RetryAllException<T>(func, retryName, retryCount, firstFastRetry);
        }

        public static async Task<T> RetryAllExceptionAsync<T>(Func<Task<T>> func, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            return await TransientFaultHandler.RetryAllExceptionAsync<T>(func, retryName, retryCount, firstFastRetry);
        }

        public static OperationResponse<bool> SafeExecute(Action action, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            return TransientFaultHandler.SafeExecute(action, failedAction, needRetry, retryName, retryCount, firstFastRetry, needLog);
        }

        public static async Task<OperationResponse<bool>> SafeExecuteAsync(Func<Task> action, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            return await TransientFaultHandler.SafeExecuteAsync(action, failedAction, needRetry, retryName, retryCount, firstFastRetry, needLog);
        }

        public static OperationResponse<T> SafeExecute<T>(Func<T> func, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            return TransientFaultHandler.SafeExecute<T>(func, failedAction, needRetry, retryName, retryCount, firstFastRetry, needLog);
        }

        public static async Task<OperationResponse<T>> SafeExecuteAsync<T>(Func<Task<T>> func, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            return await TransientFaultHandler.SafeExecuteAsync<T>(func, failedAction, needRetry, retryName, retryCount, firstFastRetry, needLog);
        }
    }
}
