using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CloudMemoryCache.Common.Logging;
using CloudMemoryCache.Contract;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace CloudMemoryCache.Common.TransientFaultHandling
{
    public class TransientFaultHandler : ITransientFaultHandler
    {
        private readonly ILogger _logger;
        public TransientFaultHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void RetryAllException(Action action, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            var retryStrategy = new ExponentialBackoff(retryName, retryCount, TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(500), firstFastRetry);
            var retryPolicy = new RetryPolicy<AllExceptionTransientErrorDetectionStrategy>(retryStrategy);
            retryPolicy.ExecuteAction(action);
        }

        public async Task RetryAllExceptionAsync(Func<Task> action, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            var retryStrategy = new ExponentialBackoff(retryName, retryCount, TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(500), firstFastRetry);
            var retryPolicy = new RetryPolicy<AllExceptionTransientErrorDetectionStrategy>(retryStrategy);
            await retryPolicy.ExecuteAsync(action);
        }

        public T RetryAllException<T>(Func<T> func, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            var retryStrategy = new ExponentialBackoff(retryName, retryCount, TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(500), firstFastRetry);
            var retryPolicy = new RetryPolicy<AllExceptionTransientErrorDetectionStrategy>(retryStrategy);
            return retryPolicy.ExecuteAction<T>(func);
        }

        public async Task<T> RetryAllExceptionAsync<T>(Func<Task<T>> func, string retryName = null, int retryCount = 3, bool firstFastRetry = true)
        {
            var retryStrategy = new ExponentialBackoff(retryName, retryCount, TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(500), firstFastRetry);
            var retryPolicy = new RetryPolicy<AllExceptionTransientErrorDetectionStrategy>(retryStrategy);
            return await retryPolicy.ExecuteAsync(func);
        }

        public OperationResponse<bool> SafeExecute(Action action, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            try
            {
                if (needRetry)
                {
                    RetryAllException(action, retryName, retryCount, firstFastRetry);
                }
                else
                {
                    action.Invoke();
                }
                return new OperationResponse<bool>
                {
                    Value = true
                };
            }
            catch (Exception ex)
            {
                if (needLog)
                {
                    _logger.Log(LogConstants.Level_Exception, MethodBase.GetCurrentMethod().Name, ex.ToString(), null);
                }
                if (failedAction != null)
                {
                    SafeExecute(failedAction, retryName: retryName, retryCount: retryCount, firstFastRetry: firstFastRetry);
                }
                return new OperationResponse<bool>
                {
                    Value = false,
                    Messages = new List<string> { ex.ToString() }
                };
            }
        }

        public async Task<OperationResponse<bool>> SafeExecuteAsync(Func<Task> action, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            try
            {
                if (needRetry)
                {
                    await RetryAllExceptionAsync(action, retryName, retryCount, firstFastRetry);
                }
                else
                {
                    await action.Invoke();
                }
                return new OperationResponse<bool>
                {
                    Value = true
                };
            }
            catch (Exception ex)
            {
                if (needLog)
                {
                    _logger.Log(LogConstants.Level_Exception, MethodBase.GetCurrentMethod().Name, ex.ToString(), null);
                }
                if (failedAction != null)
                {
                    SafeExecute(failedAction, retryName: retryName, retryCount: retryCount, firstFastRetry: firstFastRetry);
                }
                return new OperationResponse<bool>
                {
                    Value = false,
                    Messages = new List<string> { ex.ToString() }
                };
            }
        }

        public OperationResponse<T> SafeExecute<T>(Func<T> func, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            try
            {
                T result = needRetry ? RetryAllException(func, retryName, retryCount, firstFastRetry) : func.Invoke();
                return new OperationResponse<T>
                {
                    Value = result
                };
            }
            catch (Exception ex)
            {
                if (needLog)
                {
                    _logger.Log(LogConstants.Level_Exception, MethodBase.GetCurrentMethod().Name, ex.ToString(), typeof(T).FullName);
                }
                if (failedAction != null)
                {
                    SafeExecute(failedAction, retryName: retryName, retryCount: retryCount, firstFastRetry: firstFastRetry);
                }
                return new OperationResponse<T>
                {
                    Value = default(T),
                    Messages = new List<string> { ex.ToString() }
                };
            }
        }

        public async Task<OperationResponse<T>> SafeExecuteAsync<T>(Func<Task<T>> func, Action failedAction = null, bool needRetry = true, string retryName = null, int retryCount = 3, bool firstFastRetry = true, bool needLog = true)
        {
            try
            {
                T result = needRetry ? await RetryAllExceptionAsync(func, retryName, retryCount, firstFastRetry) : await func.Invoke();
                return new OperationResponse<T>
                {
                    Value = result
                };
            }
            catch (Exception ex)
            {
                if (needLog)
                {
                    _logger.Log(LogConstants.Level_Exception, MethodBase.GetCurrentMethod().Name, ex.ToString(), typeof(T).FullName);
                }
                if (failedAction != null)
                {
                    SafeExecute(failedAction, retryName: retryName, retryCount: retryCount, firstFastRetry: firstFastRetry);
                }
                return new OperationResponse<T>
                {
                    Value = default(T),
                    Messages = new List<string> { ex.ToString() }
                };
            }
        }
    }
}
