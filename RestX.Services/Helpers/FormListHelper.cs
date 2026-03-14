using RestX.BLL.DataTranferObjects.Share;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.BLL.Services;
using RestX.Models.Enum;
using RestX.Models.Tenants;
using System.ComponentModel;
using System.Reflection;

namespace RestX.BLL.Helpers;

public class FormListHelper : BaseService
{
    public FormListHelper(
        IRepository repo,
        IRedisService redisService,
        IEnumerable<ActiveTenant> tenant = null
    ) : base(repo, redisService, tenant)
    {
    }

    public async Task<List<SelectOption>> GetListByName(string name)
    {
        var cacheKey = $"FormList:{CurrentTenant.Hostname}:{name}";

        var cached = await RedisService.GetAsync<List<SelectOption>>(cacheKey);
        if (cached != null)
            return cached;

        var result = name.Trim().ToLowerInvariant() switch
        {
            // Add more enum names here
            "table-statuses" => ConvertEnumToList(typeof(TableStatus)),
            "dish-image-types" => ConvertEnumToList(typeof(DishImageType)),
            "order-statuses" => ConvertEnumToList(typeof(OrderStatus)),
            "payment-statuses" => ConvertEnumToList(typeof(PaymentStatus)),
            _ => new List<SelectOption>()
        };

        await RedisService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

        return result;
    }

    public List<SelectOption> ConvertEnumToList(Type type)
    {
        var options = new List<SelectOption>();

        foreach (Enum item in Enum.GetValues(type))
        {
            options.Add(new SelectOption
            {
                Id = ((int)Enum.Parse(type, item.ToString())).ToString(),
                Name = StringValueOfEnum(item)
            });
        }

        return options;
    }

    public string StringValueOfEnum(Enum value)
    {
        FieldInfo? fi = value.GetType().GetField(value.ToString());
        var attributes = (DescriptionAttribute[]?)fi?.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes != null && attributes.Length > 0)
            return attributes[0].Description;

        return value.ToString();
    }
}
