using System;
using System.Configuration;
using System.Linq;

namespace CloudMemoryCache.Utils
{
    public class ConfigurationUtil : IConfigurationUtil
    {
        public T TryReadAppSetting<T>(string key, T defaultValue)
        {
            if (String.IsNullOrEmpty(key) ||
                !ConfigurationManager.AppSettings.AllKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase))
            {
                return defaultValue;
            }

            string value = ConfigurationManager.AppSettings[key];
            return TypeUtil.ReadValue<T>(value, defaultValue);
        }
    }
}