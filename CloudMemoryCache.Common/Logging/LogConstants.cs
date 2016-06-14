namespace CloudMemoryCache.Common.Logging
{
    public class LogConstants
    {
        public const string Level_Exception = "Exception";
        public const string Level_Info = "Info";

        public const string Operation_AzureCacheConnected = "AzureCacheConnected";
        public const string Operation_ValidateDataKey = "ValidateDataKey";
        public const string Operation_ValidateDataKeyAsync = "ValidateDataKeyAsync";
        public const string Operation_CacheDisposed = "CacheDisposed";
        public const string Operation_InitializeCompleted = "InitializeCompleted";
        public const string Operation_SyncAzureToLocalCompleted = "SyncAzureToLocalCompleted";
        public const string Operation_Set = "Set";
        public const string Operation_SetAsync = "SetAsync";
        public const string Operation_NotSetBySizeLimit = "NotSetBySizeLimit";
        public const string Operation_NotSetBySizeLimitAsync = "NotSetBySizeLimitAsync";
        public const string Operation_Remove = "Remove";
        public const string Operation_RemoveAsync = "RemoveAsync";
        public const string Operation_Get = "Get";
        public const string Operation_GetAsync = "GetAsync";
        public const string Operation_GetKeys = "GetKeys";
        public const string Operation_Contains = "Contains";
        public const string Operation_ContainsAsync = "ContainsAsync";
        public const string Operation_GetCacheItemConfig = "GetCacheItemConfig";
        public const string Operation_GetCacheItemConfigAsync = "GetCacheItemConfigAsync";
        public const string Operation_CalculateTotalLength = "CalculateTotalLength";
        public const string Operation_CacheChanged = "CacheChanged";
        public const string Operation_CacheChangedAsync = "CacheChangedAsync";

        public const string Reserved_Cache_Key_AllLogConfigs = "AllLogConfigs";
    }
}
