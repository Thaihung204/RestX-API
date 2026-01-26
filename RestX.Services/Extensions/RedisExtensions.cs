using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;
using RestX.BLL.Interfaces;

namespace RestX.BLL.Extensions
{
    public static class RedisExtensions
    {
        public static async Task<T> GetAsync<T>(this IRedisService redisService, string key)
        {
            var cacheValue = await redisService.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cacheValue))
                return JsonConvert.DeserializeObject<T>(cacheValue);
            return default(T);
        }

        public static async Task SetAsync<T>(this IRedisService redisService, string key, T value, TimeSpan? cacheTime = null)
        {
            if (value is null) throw new ArgumentNullException("Value can not be null");
            await redisService.SetStringAsync(key, JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }), cacheTime);
        }
    }
}
