using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using CloudMemoryCache.Client.AccountManagement;
using CloudMemoryCache.Common.Logging;
using CloudMemoryCache.Common.TransientFaultHandling;
using CloudMemoryCache.Common.Utils;
using CloudMemoryCache.Contract;
using CloudMemoryCache.Utils;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CloudMemoryCache
{
    public class Cache : ICache
    {
        public Action<string> OnSyncAzureToLocalCompleted { get; set; }
        public Action<CacheChange> OnCacheChanged { get; set; }
        public Action<CacheGetResult> OnCacheGot { get; set; }
        public CacheConfig CacheConfig { get; private set; }

        public Cache()
        {
            InitializeResult = new OperationResponse<bool>
            {
                Value = false,
                Messages =
                    new List<string> { "Cache is not initialized. Please call initialize method for the cache object." }
            };
            _id = Guid.NewGuid();
            _logger = new Logger(_id);
            _transientFaultHandler = new TransientFaultHandler(_logger);
            CacheConfig = new CacheConfig();
        }

        private readonly Guid _id = Guid.NewGuid();

        public OperationResponse<bool> InitializeResult { get; private set; }

        private ConnectionMultiplexer _connection;
        private string _hostName;
        public string HostName { get { return _hostName; } }

        private IDatabase DataBase
        {
            get
            {
                return _connection.GetDatabase();
            }
        }
        private ISubscriber _subscriber;

        private MemoryCache _memoryCache;

        private IAccountManagerClient _accountManagerClient;
        private readonly ILogger _logger;
        private readonly ITransientFaultHandler _transientFaultHandler;

        private OperationResponse<bool> ValidateDataKey(Guid dataKey)
        {
            if (!String.IsNullOrWhiteSpace(_hostName))
            {
                OperationResponse<bool> validateDataKeyResponse = null;
                _transientFaultHandler.SafeExecute(
                    () => { validateDataKeyResponse = _accountManagerClient.ValidateDataKey(_hostName, dataKey); },
                    needRetry: false);
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_ValidateDataKey, dataKey.ToString(), validateDataKeyResponse);
                if (validateDataKeyResponse != null && !validateDataKeyResponse.Value)
                {
                    return validateDataKeyResponse;
                }
            }
            return new OperationResponse<bool>
            {
                Value = true
            };
        }

        private async Task<OperationResponse<bool>> ValidateDataKeyAsync(Guid dataKey)
        {
            if (!String.IsNullOrWhiteSpace(_hostName))
            {
                OperationResponse<bool> validateDataKeyResponse = null;
                await _transientFaultHandler.SafeExecuteAsync(
                    async () => { validateDataKeyResponse = await _accountManagerClient.ValidateDataKeyAsync(_hostName, dataKey); },
                    needRetry: false);
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_ValidateDataKeyAsync, dataKey.ToString(), validateDataKeyResponse);
                if (validateDataKeyResponse != null && !validateDataKeyResponse.Value)
                {
                    return validateDataKeyResponse;
                }
            }
            return new OperationResponse<bool>
            {
                Value = true,
                Messages = new List<string> { "Default operation result." }
            };
        }

        private void InitializeComponents(string connectionString, CacheConfig cacheConfig)
        {
            OnSyncAzureToLocalCompleted = cacheConfig.OnSyncAzureToLocalCompleted;
            OnCacheChanged = cacheConfig.OnCacheChanged;
            OnCacheGot = cacheConfig.OnCacheGot;

            _accountManagerClient = new AccountManagerClient(new ConfigurationUtil());

            _connection = ConnectionMultiplexer.Connect(connectionString);
            _hostName = _transientFaultHandler.SafeExecute(() => GetHostName(), needRetry: false).Value;
            _logger.HostName = _hostName;
            _logger.Log(LogConstants.Level_Info, LogConstants.Operation_AzureCacheConnected, null, cacheConfig);
            _memoryCache = new MemoryCache(connectionString,
                                           new NameValueCollection
                                            {
                                                { "cacheMemoryLimitMegabytes", cacheConfig.LocalCacheMemoryLimitInMB.ToString() },
                                                { "pollingInterval", TimeSpan.FromSeconds(cacheConfig.PollingIntervalInSeconds).ToString() },
                                            });
            _subscriber = _connection.GetSubscriber();
        }

        public void Initialize(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null)
        {
            InitializeResult = _transientFaultHandler.SafeExecute(
            () =>
            {
                if (cacheConfig == null)
                {
                    cacheConfig = new CacheConfig();
                }
                CacheConfig = cacheConfig;
                if (cacheConfig.DisableCacheUsage)
                {
                    SyncAzureToLocalCompleted = true;
                    return;
                }
                InitializeComponents(connectionString, cacheConfig);

                if (!cacheConfig.DisableMemoryUsage)
                {
                    _subscriber.Subscribe(Constants.PUBLISH_CHANNEL, (channel, message) =>
                    {
                        var cacheChange = JsonConvert.DeserializeObject<CacheChange>(message);
                        if (cacheChange.PublishClientId != _id)
                        {
                            var configKey = Constants.CONFIG_RESERVED_PREFIX + cacheChange.CacheKey;
                            switch (cacheChange.CacheChangeType)
                            {
                                case CacheChangeType.Set:
                                    var cacheItemConfigString = DataBase.StringGet(configKey);
                                    if (cacheItemConfigString.HasValue)
                                    {
                                        var cacheItemConfig =
                                            JsonConvert.DeserializeObject<CacheItemConfig>(cacheItemConfigString);
                                        var cacheItemPolicy = new CacheItemPolicy
                                        {
                                            AbsoluteExpiration = cacheItemConfig.AbsoluteExpiration
                                        };

                                        cacheItemConfig.CacheItemInfo.InMemoryTime = null;
                                        cacheItemConfig.CacheItemInfo.IsInMemory = false;
                                        if (cacheConfig.NeedSyncToMemory &&
                                            cacheItemConfig.NeedSyncToMemory &&
                                            (cacheConfig.FocusedCategories == null ||
                                             cacheConfig.FocusedCategories.Contains(cacheItemConfig.Category)) &&
                                            (cacheConfig.SizeLimitForSyncToMemoryInBytes <= 0 ||
                                             cacheConfig.SizeLimitForSyncToMemoryInBytes >=
                                             cacheItemConfig.CacheItemInfo.Length))
                                        {
                                            var value = DataBase.StringGet(cacheChange.CacheKey);
                                            if (value.HasValue)
                                            {
                                                _memoryCache.Set(cacheChange.CacheKey, value, cacheItemPolicy);
                                                cacheItemConfig.CacheItemInfo.InMemoryTime = DateTimeOffset.Now;
                                                cacheItemConfig.CacheItemInfo.IsInMemory = true;
                                            }
                                        }
                                        _memoryCache.Set(configKey, cacheItemConfig, cacheItemPolicy);
                                        cacheChange.CacheItemConfig = cacheItemConfig;
                                        _logger.Log(LogConstants.Level_Info, LogConstants.Operation_CacheChanged, "Set",
                                            cacheChange);
                                    }
                                    break;
                                case CacheChangeType.Remove:
                                    _memoryCache.Remove(cacheChange.CacheKey);
                                    _memoryCache.Remove(configKey);
                                    _logger.Log(LogConstants.Level_Info, LogConstants.Operation_CacheChanged, "Remove",
                                        cacheChange);
                                    break;
                                default:
                                    throw new NotImplementedException("No implementation for CacheChangeType " +
                                                                      cacheChange.CacheChangeType);
                            }
                        }
                        if (OnCacheChanged != null)
                        {
                            _transientFaultHandler.SafeExecute(
                                () => OnCacheChanged.Invoke(cacheChange),
                                needRetry: false);
                        }
                    });

                    Task.Factory.StartNew(() => SyncAzureToLocal(cacheConfig));
                }
                else
                {
                    SyncAzureToLocalCompleted = true;
                    if (OnSyncAzureToLocalCompleted != null)
                    {
                        _transientFaultHandler.SafeExecute(
                               () => OnSyncAzureToLocalCompleted.Invoke(_hostName),
                               needRetry: false);
                    }
                }

                if (cacheConfig.WaitForClusterReady)
                {
                    Thread.Sleep(1000);
                }
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_InitializeCompleted, null, cacheConfig);
            });

            if (InitializeResult.IsSuccessful)
            {
                InitializeResult = ValidateDataKey(dataKey ?? Guid.Empty);
            }
        }

        public async Task InitializeAsync(string connectionString, Guid? dataKey = null, CacheConfig cacheConfig = null)
        {
            InitializeResult = await _transientFaultHandler.SafeExecuteAsync(
            async () =>
            {
                if (cacheConfig == null)
                {
                    cacheConfig = new CacheConfig();
                }
                CacheConfig = cacheConfig;
                if (cacheConfig.DisableCacheUsage)
                {
                    SyncAzureToLocalCompleted = true;
                    return;
                }
                InitializeComponents(connectionString, cacheConfig);

                if (!cacheConfig.DisableMemoryUsage)
                {
                    await _subscriber.SubscribeAsync(Constants.PUBLISH_CHANNEL, async (channel, message) =>
                    {
                        var cacheChange = JsonConvert.DeserializeObject<CacheChange>(message);
                        if (cacheChange.PublishClientId != _id)
                        {
                            var configKey = Constants.CONFIG_RESERVED_PREFIX + cacheChange.CacheKey;
                            switch (cacheChange.CacheChangeType)
                            {
                                case CacheChangeType.Set:
                                    var cacheItemConfigString = await DataBase.StringGetAsync(configKey);
                                    if (cacheItemConfigString.HasValue)
                                    {
                                        var cacheItemConfig =
                                            JsonConvert.DeserializeObject<CacheItemConfig>(cacheItemConfigString);
                                        var cacheItemPolicy = new CacheItemPolicy
                                        {
                                            AbsoluteExpiration = cacheItemConfig.AbsoluteExpiration
                                        };

                                        cacheItemConfig.CacheItemInfo.InMemoryTime = null;
                                        cacheItemConfig.CacheItemInfo.IsInMemory = false;
                                        if (cacheConfig.NeedSyncToMemory &&
                                            cacheItemConfig.NeedSyncToMemory &&
                                            (cacheConfig.FocusedCategories == null ||
                                             cacheConfig.FocusedCategories.Contains(cacheItemConfig.Category)) &&
                                            (cacheConfig.SizeLimitForSyncToMemoryInBytes <= 0 ||
                                             cacheConfig.SizeLimitForSyncToMemoryInBytes >=
                                             cacheItemConfig.CacheItemInfo.Length))
                                        {
                                            var value = await DataBase.StringGetAsync(cacheChange.CacheKey);
                                            if (value.HasValue)
                                            {
                                                _memoryCache.Set(cacheChange.CacheKey, value, cacheItemPolicy);
                                                cacheItemConfig.CacheItemInfo.InMemoryTime = DateTimeOffset.Now;
                                                cacheItemConfig.CacheItemInfo.IsInMemory = true;
                                            }
                                        }
                                        _memoryCache.Set(configKey, cacheItemConfig, cacheItemPolicy);
                                        cacheChange.CacheItemConfig = cacheItemConfig;
                                        _logger.Log(LogConstants.Level_Info, LogConstants.Operation_CacheChanged, "Set",
                                            cacheChange);
                                    }
                                    break;
                                case CacheChangeType.Remove:
                                    _memoryCache.Remove(cacheChange.CacheKey);
                                    _memoryCache.Remove(configKey);
                                    _logger.Log(LogConstants.Level_Info, LogConstants.Operation_CacheChanged, "Remove",
                                        cacheChange);
                                    break;
                                default:
                                    throw new NotImplementedException("No implementation for CacheChangeType " +
                                                                      cacheChange.CacheChangeType);
                            }
                        }
                        if (OnCacheChanged != null)
                        {
                            _transientFaultHandler.SafeExecute(
                                () => OnCacheChanged.Invoke(cacheChange),
                                needRetry: false);
                        }
                    });

                    Task.Factory.StartNew(() => SyncAzureToLocal(cacheConfig));
                }
                else
                {
                    SyncAzureToLocalCompleted = true;
                    if (OnSyncAzureToLocalCompleted != null)
                    {
                        _transientFaultHandler.SafeExecute(
                               () => OnSyncAzureToLocalCompleted.Invoke(_hostName),
                               needRetry: false);
                    }
                }

                if (cacheConfig.WaitForClusterReady)
                {
                    Thread.Sleep(1000);
                }

                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_InitializeCompleted, null, cacheConfig);
            });

            if (InitializeResult.IsSuccessful)
            {
                InitializeResult = await ValidateDataKeyAsync(dataKey ?? Guid.Empty);
            }
        }

        private string GetHostName()
        {
            return (_connection.GetEndPoints()[0] as DnsEndPoint).Host;
        }

        public bool SyncAzureToLocalCompleted { get; private set; }
        private void SyncAzureToLocal(CacheConfig cacheConfig)
        {
            try
            {
                var hashKeys = ToHashSet(GetKeys(), StringComparer.InvariantCultureIgnoreCase);
                var hashPrioritizedSyncToLocalKeys = ToHashSet(cacheConfig.PrioritizedSyncToLocalKeys, StringComparer.InvariantCultureIgnoreCase);
                foreach (var key in cacheConfig.PrioritizedSyncToLocalKeys)
                {
                    if (hashKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                    {
                        SyncAzureToLocal(cacheConfig, key);
                    }
                }
                foreach (var key in hashKeys)
                {
                    if (!hashPrioritizedSyncToLocalKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                    {
                        SyncAzureToLocal(cacheConfig, key);
                    }
                }
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_SyncAzureToLocalCompleted, null, null);
            }
            finally
            {
                SyncAzureToLocalCompleted = true;
                if (OnSyncAzureToLocalCompleted != null)
                {
                    _transientFaultHandler.SafeExecute(
                           () => OnSyncAzureToLocalCompleted.Invoke(_hostName),
                           needRetry: false);
                }
            }
        }

        private HashSet<T> ToHashSet<T>(T[] items, IEqualityComparer<T> comparer)
        {
            var result = new HashSet<T>(comparer);
            foreach (var item in items)
            {
                result.Add(item);
            }
            return result;
        }

        private void SyncAzureToLocal(CacheConfig cacheConfig, string key)
        {
            try
            {
                var configKey = Constants.CONFIG_RESERVED_PREFIX + key;
                if (!DataBase.KeyExists(key))
                {
                    return;
                }
                var cacheItemConfigString = DataBase.StringGet(configKey);
                if (!cacheItemConfigString.HasValue)
                {
                    return;
                }
                var cacheItemConfig = JsonConvert.DeserializeObject<CacheItemConfig>(cacheItemConfigString);
                var cacheItemPolicy = new CacheItemPolicy
                {
                    AbsoluteExpiration = cacheItemConfig.AbsoluteExpiration
                };
                cacheItemConfig.CacheItemInfo.InMemoryTime = null;
                cacheItemConfig.CacheItemInfo.IsInMemory = false;
                if (cacheConfig.NeedSyncToMemory &&
                    cacheItemConfig.NeedSyncToMemory &&
                    (cacheConfig.FocusedCategories == null || cacheConfig.FocusedCategories.Contains(cacheItemConfig.Category)) &&
                    (cacheConfig.SizeLimitForSyncToMemoryInBytes <= 0 || cacheConfig.SizeLimitForSyncToMemoryInBytes >= cacheItemConfig.CacheItemInfo.Length))
                {
                    var value = DataBase.StringGet(key);
                    if (value.HasValue)
                    {
                        _memoryCache.Set(key, value, cacheItemPolicy);
                        cacheItemConfig.CacheItemInfo.InMemoryTime = DateTimeOffset.Now;
                        cacheItemConfig.CacheItemInfo.IsInMemory = true;
                    }
                }
                _memoryCache.Set(configKey, cacheItemConfig, cacheItemPolicy);
            }
            catch (Exception)
            {
            }
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        Task.Factory.StartNew(() => _connection.Close()).Wait(500);
                    }
                    if (_memoryCache != null)
                    {
                        _memoryCache.Dispose();
                    }
                }

                // Indicate that the instance has been disposed.
                _connection = null;
                _memoryCache = null;
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_CacheDisposed, null, null);
                disposed = true;
            }
        }

        #endregion

        private void ThrowIfNotInitialized()
        {
            if (!InitializeResult.IsSuccessful)
            {
                throw new InvalidOperationException(InitializeResult.Message);
            }
        }

        public void Set<T>(string key, T value, CacheItemConfig cacheItemConfig = null)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return;
            }
            ThrowIfNotInitialized();
            if (key.StartsWith(Constants.CONFIG_RESERVED_PREFIX))
            {
                throw new ArgumentException(String.Format("Key cannot start with {0}.", Constants.CONFIG_RESERVED_PREFIX));
            }
            if (cacheItemConfig == null)
            {
                cacheItemConfig = new CacheItemConfig();
            }
            if (cacheItemConfig.AbsoluteExpiration < DateTimeOffset.Now)
            {
                throw new InvalidOperationException(String.Format("Cannot set cache expire on a prior time {0}.", cacheItemConfig.AbsoluteExpiration));
            }

            _logger.Log(LogConstants.Level_Info, LogConstants.Operation_Set, key, cacheItemConfig);
            var configKey = Constants.CONFIG_RESERVED_PREFIX + key;
            if (value != null)
            {
                _transientFaultHandler.SafeExecute(() =>
                {
                    string valueToSet = (value is string) ? value as string : JsonConvert.SerializeObject(value);
                    if (CacheConfig.SizeLimitForSetCacheInBytes > 0 &&
                        CacheConfig.SizeLimitForSetCacheInBytes < valueToSet.Length)
                    {
                        _logger.Log(LogConstants.Level_Info, LogConstants.Operation_NotSetBySizeLimit, key, valueToSet.Length);
                        return;
                    }
                    bool isCompressed = false;
                    if (valueToSet.Length > Constants.COMPRESS_LENGTH_THREASHOLD)
                    {
                        var compressedValue = CompressUtil.Compress(valueToSet);
                        if (compressedValue.Length < valueToSet.Length && CompressUtil.Decompress(compressedValue) == valueToSet)
                        {
                            valueToSet = Constants.COMPRESS_RESERVED_PREFIX + compressedValue;
                            isCompressed = true;
                        }
                    }
                    cacheItemConfig.CacheItemInfo.SetTime = DateTimeOffset.Now;
                    cacheItemConfig.CacheItemInfo.SetMachineName = Environment.MachineName;
                    cacheItemConfig.CacheItemInfo.SetUserName = Environment.UserName;
                    cacheItemConfig.CacheItemInfo.Length = valueToSet.Length;
                    cacheItemConfig.CacheItemInfo.IsCompressed = isCompressed;
                    cacheItemConfig.CacheItemInfo.ValueTypeName = typeof(T).FullName;
                    cacheItemConfig.CacheItemInfo.IsInMemory = true;


                    var cacheItemPolicy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = cacheItemConfig.AbsoluteExpiration
                    };
                    _memoryCache.Set(key, valueToSet, cacheItemPolicy);
                    cacheItemConfig.CacheItemInfo.InMemoryTime = DateTimeOffset.Now;
                    _memoryCache.Set(configKey, cacheItemConfig, cacheItemPolicy);

                    var expire = cacheItemConfig.AbsoluteExpiration == DateTimeOffset.MaxValue
                        ? (TimeSpan?)null
                        : cacheItemConfig.AbsoluteExpiration - DateTimeOffset.Now;
                    var configString = JsonConvert.SerializeObject(cacheItemConfig);
                    var tran = DataBase.CreateTransaction();
                    tran.StringSetAsync(configKey, configString, expire);
                    tran.StringSetAsync(key, valueToSet, expire);
                    if (!tran.Execute())
                    {
                        throw new InvalidProgramException(String.Format("Azure cache operation Set is failed for \"{0}\".", key));
                    }
                    else
                    {
                        _subscriber.Publish(Constants.PUBLISH_CHANNEL, JsonConvert.SerializeObject(new CacheChange
                        {
                            CacheChangeType = CacheChangeType.Set,
                            PublishClientId = _id,
                            CacheKey = key,
                            CacheItemConfig = cacheItemConfig
                        }));
                    }
                });
            }
            else
            {
                Remove(key);
            }
        }

        public async Task SetAsync<T>(string key, T value, CacheItemConfig cacheItemConfig = null)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return;
            }
            ThrowIfNotInitialized();
            if (key.StartsWith(Constants.CONFIG_RESERVED_PREFIX))
            {
                throw new ArgumentException(String.Format("Key cannot start with {0}.", Constants.CONFIG_RESERVED_PREFIX));
            }
            if (cacheItemConfig == null)
            {
                cacheItemConfig = new CacheItemConfig();
            }
            if (cacheItemConfig.AbsoluteExpiration < DateTimeOffset.Now)
            {
                throw new InvalidOperationException(String.Format("Cannot set cache expire on a prior time {0}.", cacheItemConfig.AbsoluteExpiration));
            }

            _logger.Log(LogConstants.Level_Info, LogConstants.Operation_SetAsync, key, cacheItemConfig);
            var configKey = Constants.CONFIG_RESERVED_PREFIX + key;
            if (value != null)
            {
                await _transientFaultHandler.SafeExecuteAsync(async () =>
                {
                    string valueToSet = (value is string) ? value as string : JsonConvert.SerializeObject(value);
                    if (CacheConfig.SizeLimitForSetCacheInBytes > 0 &&
                        CacheConfig.SizeLimitForSetCacheInBytes < valueToSet.Length)
                    {
                        _logger.Log(LogConstants.Level_Info, LogConstants.Operation_NotSetBySizeLimitAsync, key, valueToSet.Length);
                        return;
                    }

                    bool isCompressed = false;
                    if (valueToSet.Length > Constants.COMPRESS_LENGTH_THREASHOLD)
                    {
                        var compressedValue = CompressUtil.Compress(valueToSet);
                        if (compressedValue.Length < valueToSet.Length && CompressUtil.Decompress(compressedValue) == valueToSet)
                        {
                            valueToSet = Constants.COMPRESS_RESERVED_PREFIX + compressedValue;
                            isCompressed = true;
                        }
                    }

                    cacheItemConfig.CacheItemInfo.SetTime = DateTimeOffset.Now;
                    cacheItemConfig.CacheItemInfo.SetMachineName = Environment.MachineName;
                    cacheItemConfig.CacheItemInfo.SetUserName = Environment.UserName;
                    cacheItemConfig.CacheItemInfo.Length = valueToSet.Length;
                    cacheItemConfig.CacheItemInfo.IsCompressed = isCompressed;
                    cacheItemConfig.CacheItemInfo.ValueTypeName = typeof(T).FullName;
                    cacheItemConfig.CacheItemInfo.IsInMemory = true;

                    var cacheItemPolicy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = cacheItemConfig.AbsoluteExpiration
                    };
                    _memoryCache.Set(key, valueToSet, cacheItemPolicy);
                    cacheItemConfig.CacheItemInfo.InMemoryTime = DateTimeOffset.Now;
                    _memoryCache.Set(configKey, cacheItemConfig, cacheItemPolicy);

                    var expire = cacheItemConfig.AbsoluteExpiration == DateTimeOffset.MaxValue
                        ? (TimeSpan?)null
                        : cacheItemConfig.AbsoluteExpiration - DateTimeOffset.Now;
                    var configString = JsonConvert.SerializeObject(cacheItemConfig);
                    var tran = DataBase.CreateTransaction();
                    tran.StringSetAsync(configKey, configString, expire);
                    tran.StringSetAsync(key, valueToSet, expire);

                    if (!await tran.ExecuteAsync())
                    {
                        throw new InvalidProgramException(
                            String.Format("Azure cache operation Set is failed for \"{0}\".", key));
                    }
                    else
                    {
                        await
                            _subscriber.PublishAsync(Constants.PUBLISH_CHANNEL,
                                JsonConvert.SerializeObject(new CacheChange
                                {
                                    CacheChangeType = CacheChangeType.Set,
                                    PublishClientId = _id,
                                    CacheKey = key,
                                    CacheItemConfig = cacheItemConfig
                                }));
                    }
                });
            }
            else
            {
                await RemoveAsync(key);
            }
        }

        public void Remove(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return;
            }
            ThrowIfNotInitialized();
            _logger.Log(LogConstants.Level_Info, LogConstants.Operation_Remove, key, null);
            var configKey = Constants.CONFIG_RESERVED_PREFIX + key;
            if (_transientFaultHandler.SafeExecute(() =>
            {
                _memoryCache.Remove(key);
                _memoryCache.Remove(configKey);
                DataBase.KeyDelete(key);
                DataBase.KeyDelete(configKey);
            }).Value)
            {
                _subscriber.Publish(Constants.PUBLISH_CHANNEL, JsonConvert.SerializeObject(new CacheChange
                {
                    CacheChangeType = CacheChangeType.Remove,
                    PublishClientId = _id,
                    CacheKey = key,
                    CacheItemConfig = null
                }));
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return;
            }
            ThrowIfNotInitialized();
            _logger.Log(LogConstants.Level_Info, LogConstants.Operation_RemoveAsync, key, null);
            var configKey = Constants.CONFIG_RESERVED_PREFIX + key;
            if ((await _transientFaultHandler.SafeExecuteAsync(async () =>
            {
                _memoryCache.Remove(key);
                _memoryCache.Remove(configKey);
                await DataBase.KeyDeleteAsync(key);
                await DataBase.KeyDeleteAsync(configKey);
            })).Value)
            {
                await _subscriber.PublishAsync(Constants.PUBLISH_CHANNEL, JsonConvert.SerializeObject(new CacheChange
                {
                    CacheChangeType = CacheChangeType.Remove,
                    PublishClientId = _id,
                    CacheKey = key,
                    CacheItemConfig = null
                }));
            }
        }

        public T Get<T>(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return default(T);
            }
            ThrowIfNotInitialized();
            var stopwatch = Stopwatch.StartNew();
            var cacheGetResult = new CacheGetResult
            {
                Key = key,
                ValueTypeName = typeof(T).FullName
            };
            try
            {
                return GetInternal<T>(key, cacheGetResult);
            }
            finally
            {
                stopwatch.Stop();
                cacheGetResult.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                if (!String.Equals(key, LogConstants.Reserved_Cache_Key_AllLogConfigs))
                {
                    _logger.Log(LogConstants.Level_Info, LogConstants.Operation_Get, key, cacheGetResult);
                }
                if (OnCacheGot != null)
                {
                    _transientFaultHandler.SafeExecute(
                        () => OnCacheGot.Invoke(cacheGetResult),
                        needRetry: false);
                }
            }
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return default(T);
            }
            ThrowIfNotInitialized();
            var stopwatch = Stopwatch.StartNew();
            var cacheGetResult = new CacheGetResult
            {
                Key = key,
                ValueTypeName = typeof(T).FullName
            };
            try
            {
                return await GetInternalAsync<T>(key, cacheGetResult);
            }
            finally
            {
                stopwatch.Stop();
                cacheGetResult.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_GetAsync, key, cacheGetResult);
                if (OnCacheGot != null)
                {
                    _transientFaultHandler.SafeExecute(
                        () => OnCacheGot.Invoke(cacheGetResult),
                        needRetry: false);
                }
            }
        }

        private T GetInternal<T>(string key, CacheGetResult cacheGetResult)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var cachedValue = _transientFaultHandler.SafeExecute<string>(() =>
                {
                    if (_memoryCache.Contains(key))
                    {
                        cacheGetResult.IsInMemory = true;
                        var result = _memoryCache.Get(key);
                        return result == null ? null : result.ToString();
                    }
                    var configKey = Constants.CONFIG_RESERVED_PREFIX + key;
                    if ((_memoryCache.Contains(configKey) || CacheConfig.DisableMemoryUsage) && !CacheConfig.NotGetCacheValueFromCloud)
                    {
                        return DataBase.StringGet(key);
                    }
                    else
                    {
                        return null;
                    }
                }).Value;
                if (cachedValue == null)
                {
                    cacheGetResult.IsValueNull = true;
                    return ConvertTo<T>(null);
                }
                if (!cachedValue.StartsWith(Constants.COMPRESS_RESERVED_PREFIX))
                {
                    return _transientFaultHandler.SafeExecute(() => ConvertTo<T>(cachedValue)).Value;
                }

                return _transientFaultHandler.SafeExecute(() =>
                {
                    var cachedValueWithoutPrefix = cachedValue.Substring(Constants.COMPRESS_RESERVED_PREFIX.Length);
                    var value = CompressUtil.Decompress(cachedValueWithoutPrefix);
                    if (value.Length <= cachedValueWithoutPrefix.Length)
                    {
                        cacheGetResult.IsCompressed = false;
                        return ConvertTo<T>(cachedValue);
                    }
                    var cacheItemConfig = GetCacheItemConfig(key);
                    if (cacheItemConfig == null)
                    {
                        cacheGetResult.IsCompressed = true;
                        return ConvertTo<T>(value);
                    }
                    cacheGetResult.IsCompressed = cacheItemConfig.CacheItemInfo.IsCompressed;
                    return cacheItemConfig.CacheItemInfo.IsCompressed ? ConvertTo<T>(value) : ConvertTo<T>(cachedValue);
                }).Value;
            }
            finally
            {
                stopwatch.Stop();
                cacheGetResult.CacheElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        private async Task<T> GetInternalAsync<T>(string key, CacheGetResult cacheGetResult)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var cachedValue = (await _transientFaultHandler.SafeExecuteAsync<string>(async () =>
                {
                    if (_memoryCache.Contains(key))
                    {
                        cacheGetResult.IsInMemory = true;
                        var result = _memoryCache.Get(key);
                        return result == null ? null : result.ToString();
                    }
                    var configKey = Constants.CONFIG_RESERVED_PREFIX + key;
                    if ((_memoryCache.Contains(configKey) || CacheConfig.DisableMemoryUsage) && !CacheConfig.NotGetCacheValueFromCloud)
                    {
                        return await DataBase.StringGetAsync(key);
                    }
                    else
                    {
                        return null;
                    }
                })).Value;
                if (cachedValue == null)
                {
                    cacheGetResult.IsValueNull = true;
                    return ConvertTo<T>(null);
                }
                if (!cachedValue.StartsWith(Constants.COMPRESS_RESERVED_PREFIX))
                {
                    return _transientFaultHandler.SafeExecute(() => ConvertTo<T>(cachedValue)).Value;
                }
                return (await _transientFaultHandler.SafeExecuteAsync(async () =>
                {
                    var cachedValueWithoutPrefix = cachedValue.Substring(Constants.COMPRESS_RESERVED_PREFIX.Length);
                    var value = CompressUtil.Decompress(cachedValueWithoutPrefix);
                    if (value.Length <= cachedValueWithoutPrefix.Length)
                    {
                        cacheGetResult.IsCompressed = false;
                        return ConvertTo<T>(cachedValue);
                    }
                    var cacheItemConfig = await GetCacheItemConfigAsync(key);
                    if (cacheItemConfig == null)
                    {
                        cacheGetResult.IsCompressed = true;
                        return ConvertTo<T>(value);
                    }
                    cacheGetResult.IsCompressed = cacheItemConfig.CacheItemInfo.IsCompressed;
                    return cacheItemConfig.CacheItemInfo.IsCompressed ? ConvertTo<T>(value) : ConvertTo<T>(cachedValue);
                })).Value;
            }
            finally
            {
                stopwatch.Stop();
                cacheGetResult.CacheElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        public T GetOrRetrieve<T>(string key, Func<T> retriever, CacheItemConfig cacheItemConfig = null)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return retriever();
            }
            ThrowIfNotInitialized();
            var stopwatch = Stopwatch.StartNew();
            var cacheGetResult = new CacheGetResult
            {
                Key = key,
                ValueTypeName = typeof(T).FullName
            };
            try
            {
                var result = GetInternal<T>(key, cacheGetResult);

                if (EqualityComparer<T>.Default.Equals(result, default(T)))
                {
                    var retrieverStopwatch = Stopwatch.StartNew();
                    try
                    {
                        result = retriever();
                    }
                    finally
                    {
                        retrieverStopwatch.Stop();
                        cacheGetResult.RetrievingElapsedMilliseconds = retrieverStopwatch.ElapsedMilliseconds;
                    }

                    Task.Run(() =>
                    {
                        if (!EqualityComparer<T>.Default.Equals(result, default(T)))
                        {
                            Set(key, result, cacheItemConfig);
                        }
                    });
                }
                return result;
            }
            finally
            {
                stopwatch.Stop();
                cacheGetResult.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                if (!String.Equals(key, LogConstants.Reserved_Cache_Key_AllLogConfigs))
                {
                    _logger.Log(LogConstants.Level_Info, LogConstants.Operation_Get, key, cacheGetResult);
                }
                if (OnCacheGot != null)
                {
                    _transientFaultHandler.SafeExecute(
                        () => OnCacheGot.Invoke(cacheGetResult),
                        needRetry: false);
                }
            }
        }

        public async Task<T> GetOrRetrieveAsync<T>(string key, Func<Task<T>> retriever, CacheItemConfig cacheItemConfig = null)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return await retriever();
            }
            ThrowIfNotInitialized();
            var stopwatch = Stopwatch.StartNew();
            var cacheGetResult = new CacheGetResult
            {
                Key = key,
                ValueTypeName = typeof(T).FullName
            };
            try
            {
                var result = await GetInternalAsync<T>(key, cacheGetResult);

                if (EqualityComparer<T>.Default.Equals(result, default(T)))
                {
                    var retrieverStopwatch = Stopwatch.StartNew();
                    try
                    {
                        result = await retriever();
                    }
                    finally
                    {
                        retrieverStopwatch.Stop();
                        cacheGetResult.RetrievingElapsedMilliseconds = retrieverStopwatch.ElapsedMilliseconds;
                    }

                    if (!EqualityComparer<T>.Default.Equals(result, default(T)))
                    {
                        SetAsync(key, result, cacheItemConfig);
                    }
                }
                return result;
            }
            finally
            {
                stopwatch.Stop();
                cacheGetResult.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                if (!String.Equals(key, LogConstants.Reserved_Cache_Key_AllLogConfigs))
                {
                    _logger.Log(LogConstants.Level_Info, LogConstants.Operation_Get, key, cacheGetResult);
                }
                if (OnCacheGot != null)
                {
                    _transientFaultHandler.SafeExecute(
                        () => OnCacheGot.Invoke(cacheGetResult),
                        needRetry: false);
                }
            }
        }

        private T ConvertTo<T>(string value)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            if (value == null)
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(value);
        }

        public string[] GetKeys()
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return new string[0];
            }
            try
            {
                return _transientFaultHandler.SafeExecute(() =>
                {
                    var endpoints = _connection.GetEndPoints();
                    var server = _connection.GetServer(endpoints.First());
                    return server.Keys(pattern: "[^_]*").Select(key => key.ToString()).OrderBy(key => key).ToArray();
                }).Value;
            }
            finally
            {
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_GetKeys, null, null);
            }
        }

        public async Task<string[]> GetKeysAsync()
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return new string[0];
            }
            return await Task.FromResult(GetKeys());
        }

        public bool Contains(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return false;
            }
            ThrowIfNotInitialized();
            try
            {
                return _transientFaultHandler.SafeExecute(() =>
                {
                    if (_memoryCache.Contains(key))
                    {
                        return true;
                    }
                    return DataBase.KeyExists(key);
                }).Value;
            }
            finally
            {
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_Contains, key, null);
            }
        }

        public async Task<bool> ContainsAsync(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return false;
            }
            ThrowIfNotInitialized();
            try
            {
                return (await _transientFaultHandler.SafeExecuteAsync(async () =>
                {
                    if (_memoryCache.Contains(key))
                    {
                        return true;
                    }
                    return await DataBase.KeyExistsAsync(key);
                })).Value;
            }
            finally
            {
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_ContainsAsync, key, null);
            }
        }

        public CacheItemConfig GetCacheItemConfig(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return null;
            }
            ThrowIfNotInitialized();
            if (key.StartsWith(Constants.CONFIG_RESERVED_PREFIX))
            {
                throw new ArgumentException(String.Format("Key cannot start with {0}.", Constants.CONFIG_RESERVED_PREFIX));
            }

            CacheItemConfig result = null;

            var configKey = Constants.CONFIG_RESERVED_PREFIX + key;

            return _transientFaultHandler.SafeExecute(() =>
            {
                if (_memoryCache.Contains(configKey))
                {
                    result = _memoryCache.Get(configKey) as CacheItemConfig;
                }

                if (result == null)
                {
                    var configStr = DataBase.StringGet(configKey);
                    if (!configStr.HasValue)
                    {
                        return null;
                    }
                    result = JsonConvert.DeserializeObject<CacheItemConfig>(configStr);
                }

                result.CacheItemInfo.IsInMemory = _memoryCache.Contains(key);
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_GetCacheItemConfig, key, result);
                return result;
            }).Value;
        }

        public async Task<CacheItemConfig> GetCacheItemConfigAsync(string key)
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return null;
            }
            ThrowIfNotInitialized();
            if (key.StartsWith(Constants.CONFIG_RESERVED_PREFIX))
            {
                throw new ArgumentException(String.Format("Key cannot start with {0}.", Constants.CONFIG_RESERVED_PREFIX));
            }

            CacheItemConfig result = null;

            var configKey = Constants.CONFIG_RESERVED_PREFIX + key;

            return (await _transientFaultHandler.SafeExecuteAsync(async () =>
            {
                if (_memoryCache.Contains(configKey))
                {
                    result = _memoryCache.Get(configKey) as CacheItemConfig;
                }

                if (result == null)
                {
                    var configStr = await DataBase.StringGetAsync(configKey);
                    if (!configStr.HasValue)
                    {
                        return null;
                    }
                    result = JsonConvert.DeserializeObject<CacheItemConfig>(configStr);
                }

                result.CacheItemInfo.IsInMemory = _memoryCache.Contains(key);
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_GetCacheItemConfigAsync, key, result);
                return result;
            })).Value;
        }

        public int CalculateTotalLength()
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return 0;
            }
            ThrowIfNotInitialized();
            return _transientFaultHandler.SafeExecute(() =>
            {
                var configKeys = _memoryCache.Where(item => item.Key.StartsWith(Constants.CONFIG_RESERVED_PREFIX)).Select(item => item.Key).ToArray();
                int result = 0;
                foreach (var configKey in configKeys)
                {
                    var cacheItemConfig = _memoryCache.Get(configKey) as CacheItemConfig;
                    if (cacheItemConfig != null)
                    {
                        result += cacheItemConfig.CacheItemInfo.Length;
                    }
                }
                _logger.Log(LogConstants.Level_Info, LogConstants.Operation_CalculateTotalLength, null, result);
                return result;
            }).Value;
        }

        public async Task<int> CalculateTotalLengthAsync()
        {
            if (CacheConfig.DisableCacheUsage)
            {
                return 0;
            }
            return await Task.FromResult(CalculateTotalLength());
        }
    }
}