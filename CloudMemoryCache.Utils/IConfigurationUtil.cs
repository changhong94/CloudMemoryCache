namespace CloudMemoryCache.Utils
{
    public interface IConfigurationUtil
    {
        T TryReadAppSetting<T>(string key, T defaultValue);
    }
}
