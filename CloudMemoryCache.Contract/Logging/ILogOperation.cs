namespace CloudMemoryCache.Contract.Logging
{
    public interface ILogOperation
    {
        OperationResponse<bool> Log(LogItem logItem);
    }
}
