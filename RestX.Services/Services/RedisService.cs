using Microsoft.Extensions.Options;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestX.BLL;

namespace RestX.BLL.Services
{
    public class RedisService : IRedisService
    {
        private static readonly object connectLock = new object();
        private readonly AppSettings settings;
        private static Lazy<ConnectionMultiplexer>? _connection;
        private static string? _connectionString;
        private bool _disposed = false;

        public RedisService(IOptions<ConnectionStrings> connectionStrings, IOptions<AppSettings> settings)
        {
            this.settings = settings.Value;

            // Use single shared connection (recommended by StackExchange.Redis)
            if (_connection == null || _connectionString != connectionStrings.Value.RedisConnection)
            {
                lock (connectLock)
                {
                    if (_connection == null || _connectionString != connectionStrings.Value.RedisConnection)
                    {
                        _connectionString = connectionStrings.Value.RedisConnection;
                        _connection = new Lazy<ConnectionMultiplexer>(() =>
                        {
                            var options = ConfigurationOptions.Parse(_connectionString);
                            options.AbortOnConnectFail = false;
                            options.ConnectTimeout = 5000;
                            options.SyncTimeout = 5000;
                            options.AsyncTimeout = 5000;
                            return ConnectionMultiplexer.Connect(options);
                        });
                    }
                }
            }
        }

        private static ConnectionMultiplexer RedisDatabase => _connection!.Value;

        /// <summary>
        /// Release all resources associated with this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Single connection disposal
                if (_connection != null && _connection.IsValueCreated)
                {
                    _connection.Value.Dispose();
                    _connection = null;
                }
            }
            _disposed = true;
        }

        public string GetString(string key)
        {
            try
            {
                if (this.settings.UseLargeCacheLogic)
                {
                    return GetLargeString(key);
                }

                return RedisDatabase.GetDatabase().StringGet(key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache GET Error");
            }

            return string.Empty;
        }

        public void SetString(string key, string value, TimeSpan? cacheTime = null)
        {
            try
            {
                if (this.settings.UseLargeCacheLogic)
                {
                    SetLargeString(key, value, cacheTime);
                }
                else
                {
                    RedisDatabase.GetDatabase().StringSet(key, value, (Expiration)cacheTime);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache SET Error");
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await RedisDatabase.GetDatabase().KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache DELETE Error");
            }
        }

        public void Remove(string key)
        {
            try
            {
                RedisDatabase.GetDatabase().KeyDelete(key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache DELETE Error");
            }
        }

        public async Task<string> GetStringAsync(string key)
        {
            try
            {
                if (this.settings.UseLargeCacheLogic)
                {
                    return await GetLargeStringAsync(key);
                }

                return await RedisDatabase.GetDatabase().StringGetAsync(key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache GET Error");
            }

            return string.Empty;
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? cacheTime = null)
        {
            try
            {
                if (this.settings.UseLargeCacheLogic)
                {
                    await SetLargeStringAsync(key, value, cacheTime);
                }
                else
                {
                    await RedisDatabase.GetDatabase().StringSetAsync(key, value, (Expiration)cacheTime);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache SET Error");
            }
        }

        public async Task<List<string>> GetAllKeys()
        {
            try
            {
                var result = new List<KeyValuePair<string, object>>();
                var endpoints = RedisDatabase.GetDatabase().Multiplexer.GetEndPoints();
                var server = RedisDatabase.GetDatabase().Multiplexer.GetServer(endpoints.First());

                return await Task.FromResult(server.Keys().Select(k => k.ToString()).ToList());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache GET ALL KEYS Error");
            }

            return new List<string>();
        }

        public async Task<List<string>> GetAllKeys(string patternSearch)
        {
            try
            {
                var result = new List<KeyValuePair<string, object>>();
                var endpoints = RedisDatabase.GetDatabase().Multiplexer.GetEndPoints();
                var server = RedisDatabase.GetDatabase().Multiplexer.GetServer(endpoints.First());

                return await Task.FromResult(server.Keys(pattern: $"*{patternSearch}*").Select(k => k.ToString()).ToList());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Redis Cache GET ALL KEYS Error");
            }

            return new List<string>();
        }

        private void SetLargeString(string key, string value, TimeSpan? cacheTime = null, int chunkSize = 200000)
        {
            if (chunkSize < 1) throw new ArgumentException("Chunk size must be greater than 0.", nameof(chunkSize));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var valueBytes = Encoding.UTF8.GetBytes(value);

            // Divide the value into chunks.
            var chunks = new List<byte[]>(valueBytes.Length / chunkSize + 1);
            var i = 0;
            while (i < valueBytes.Length)
            {
                var remainingLength = valueBytes.Length - i;
                var chunkLength = remainingLength > chunkSize ? chunkSize : remainingLength;
                var chunk = new byte[chunkLength];
                Array.Copy(valueBytes, i, chunk, 0, chunkLength);
                chunks.Add(chunk);
                i += chunkSize;
            }

            var transaction = RedisDatabase.GetDatabase().CreateTransaction();
            transaction.StringSetAsync(key, "chunk_count:" + chunks.Count.ToString(), (Expiration)cacheTime);
            for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                transaction.StringSetAsync(key + ":chunk_" + chunkIndex.ToString(), Encoding.UTF8.GetString(chunks[chunkIndex]), (Expiration)cacheTime);
            }
            transaction.ExecuteAsync();
        }
        private string GetLargeString(string key)
        {
            var redisDb = RedisDatabase.GetDatabase();
            var chunkCountString = redisDb.StringGet(key);
            if (string.IsNullOrEmpty(chunkCountString)) return null;
            var chunkCount = int.Parse(chunkCountString.ToString().Split(':')[1]);
            if (chunkCount <= 0) return null;

            var chunks = new List<byte[]>();
            for (int i = 0; i < chunkCount; i++)
            {
                var chunk = redisDb.StringGet(key + ":chunk_" + i.ToString());
                if (chunk.HasValue)
                {
                    chunks.Add(Encoding.UTF8.GetBytes(chunk.ToString()));
                }
            }

            if (chunks.Count == 0) return null;

            var valueBytes = new byte[chunks.Sum(c => c.Length)];
            int offset = 0;
            foreach (var chunk in chunks)
            {
                Array.Copy(chunk, 0, valueBytes, offset, chunk.Length);
                offset += chunk.Length;
            }
            return Encoding.UTF8.GetString(valueBytes);
        }
        private async Task SetLargeStringAsync(string key, string value, TimeSpan? cacheTime = null, int chunkSize = 200000)
        {
            if (chunkSize < 1) throw new ArgumentException("Chunk size must be greater than 0.", nameof(chunkSize));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var valueBytes = Encoding.UTF8.GetBytes(value);

            // Divide the value into chunks.
            var chunks = new List<byte[]>(valueBytes.Length / chunkSize + 1);
            var i = 0;
            while (i < valueBytes.Length)
            {
                var remainingLength = valueBytes.Length - i;
                var chunkLength = remainingLength > chunkSize ? chunkSize : remainingLength;
                var chunk = new byte[chunkLength];
                Array.Copy(valueBytes, i, chunk, 0, chunkLength);
                chunks.Add(chunk);
                i += chunkSize;
            }

            var redisDb = RedisDatabase.GetDatabase();
            var transaction = redisDb.CreateTransaction();

            transaction.StringSetAsync(key, "chunk_count:" + chunks.Count.ToString(), (Expiration)cacheTime);
            for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                transaction.StringSetAsync(key + ":chunk_" + chunkIndex.ToString(), Encoding.UTF8.GetString(chunks[chunkIndex]), (Expiration)cacheTime);
            }

            bool success = await transaction.ExecuteAsync();
            if (!success)
            {
                // Cleanup using the database, not the transaction
                var keysToDelete = new List<RedisKey> { key };
                for (int j = 0; j < chunks.Count; j++)
                {
                    keysToDelete.Add($"{key}:chunk_{j}");
                }
                await redisDb.KeyDeleteAsync(keysToDelete.ToArray());
            }
        }

        private async Task<string> GetLargeStringAsync(string key)
        {
            var redisDb = RedisDatabase.GetDatabase();
            var chunkCountString = await redisDb.StringGetAsync(key);
            if (string.IsNullOrEmpty(chunkCountString)) return null;
            var chunkCount = int.Parse(chunkCountString.ToString().Split(':')[1]);
            if (chunkCount <= 0) return null;

            // Fetch all chunks in parallel using batch operations
            var chunkKeys = new RedisKey[chunkCount];
            for (int i = 0; i < chunkCount; i++)
            {
                chunkKeys[i] = $"{key}:chunk_{i}";
            }

            var chunkValues = await redisDb.StringGetAsync(chunkKeys);

            // Validate all chunks are present
            var chunks = new byte[chunkCount][];
            for (int i = 0; i < chunkCount; i++)
            {
                if (!chunkValues[i].HasValue)
                    throw new InvalidOperationException($"Missing chunk at index {i} for key {key}.");
                chunks[i] = Encoding.UTF8.GetBytes(chunkValues[i].ToString());
            }

            var valueBytes = new byte[chunks.Sum(c => c.Length)];
            int offset = 0;
            foreach (var chunk in chunks)
            {
                Array.Copy(chunk, 0, valueBytes, offset, chunk.Length);
                offset += chunk.Length;
            }
            return Encoding.UTF8.GetString(valueBytes);
        }

        // Alias methods for convenience
        public async Task<string> GetAsync(string key) => await GetStringAsync(key);
        public async Task SetAsync(string key, string value, TimeSpan? cacheTime = null) => await SetStringAsync(key, value, cacheTime);
        public async Task DeleteAsync(string key) => await RemoveAsync(key);
    }
}
