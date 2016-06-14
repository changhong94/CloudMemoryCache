using CloudMemoryCache.Utils;

namespace CloudMemoryCache.Client
{
    public abstract class ClientBase
    {
        protected readonly string CloudMemoryCacheWebUrl;

        protected ClientBase(IConfigurationUtil configurationUtil)
        {
            //todo change the url to production
            //todo in config of CloudMemoryCache.Client.IntegrationTest.csproj, change ConnectionString
            CloudMemoryCacheWebUrl = configurationUtil.TryReadAppSetting("CloudMemoryCacheWebUrl", "http://cloudmemorycache.azurewebsites.net/");
        }
    }
}
