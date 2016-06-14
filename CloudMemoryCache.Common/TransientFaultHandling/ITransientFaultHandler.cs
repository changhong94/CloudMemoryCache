using System;
using System.Threading.Tasks;
using CloudMemoryCache.Contract;

namespace CloudMemoryCache.Common.TransientFaultHandling
{
    public interface ITransientFaultHandler
    {
        void RetryAllException(Action action, string retryName = null, int retryCount = 3, bool firstFastRetry = true);
        Task RetryAllExceptionAsync(Func<Task> action, string retryName = null, int retryCount = 3, bool firstFastRetry = true);
        T RetryAllException<T>(Func<T> func, string retryName = null, int retryCount = 3, bool firstFastRetry = true);
        Task<T> RetryAllExceptionAsync<T>(Func<Task<T>> func, string retryName = null, int retryCount = 3, bool firstFastRetry = true);
        OperationResponse<bool> SafeExecute(Action action, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true);
        Task<OperationResponse<bool>> SafeExecuteAsync(Func<Task> action, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true);
        OperationResponse<T> SafeExecute<T>(Func<T> func, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true);
        Task<OperationResponse<T>> SafeExecuteAsync<T>(Func<Task<T>> func, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true);
    }
}