using System;

namespace CloudMemoryCache.Utils
{
    public static class TypeUtil
    {
        public static T ReadValue<T>(object value, T defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }
            if (typeof(T).IsEnum)
            {
                return (T)Enum.Parse(typeof(T), value.ToString());
            }
            else
            {
                Type underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                try
                {
                    return (value is T)
                    ? (T)value
                    : (T)Convert.ChangeType(value, underlyingType);
                }
                catch (Exception)
                {
                    return defaultValue;
                }

            }
        }
    }
}
