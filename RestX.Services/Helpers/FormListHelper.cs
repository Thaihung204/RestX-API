using RestX.BLL.DataTranferObjects.Share;
using RestX.Models.Enum;
using System.ComponentModel;
using System.Reflection;

namespace RestX.BLL.Helpers;

public static class FormListHelper
{
    public static List<SelectOption> GetListByName(string name)
    {
        return name.Trim().ToLowerInvariant() switch
        {
            // Add more enum names here
            "table-statuses" => ConvertEnumToList(typeof(TableStatus)),
            "dish-image-types" => ConvertEnumToList(typeof(DishImageType)),
            "order-statuses" => ConvertEnumToList(typeof(OrderStatus))
        };
    }

    public static List<SelectOption> ConvertEnumToList(Type type)
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

    public static string StringValueOfEnum(Enum value)
    {
        FieldInfo? fi = value.GetType().GetField(value.ToString());
        var attributes = (DescriptionAttribute[]?)fi?.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes != null && attributes.Length > 0)
            return attributes[0].Description;

        return value.ToString();
    }
}