namespace RestX.BLL.Interfaces
{
    public interface IRedisService
    {
        string GetString(string key);
        Task<string> GetStringAsync(string key);

        void SetString(string key, string value, TimeSpan? cacheTime = null);
        Task SetStringAsync(string key, string value, TimeSpan? cacheTime = null);
        
        Task RemoveAsync(string key);
        void Remove(string key);
        Task<List<string>> GetAllKeys();
        Task<List<string>> GetAllKeys(string patternSearch);
    }
}
