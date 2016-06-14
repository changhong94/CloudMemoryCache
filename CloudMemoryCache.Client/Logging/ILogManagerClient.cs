using System.Threading.Tasks;
using CloudMemoryCache.Contract;
using CloudMemoryCache.Contract.Logging;

namespace CloudMemoryCache.Client.Logging
{
    public interface ILogManagerClient : ILogOperation
    {
        Task<OperationResponse<bool>> LogAsync(LogItem logItem);
    }
}
