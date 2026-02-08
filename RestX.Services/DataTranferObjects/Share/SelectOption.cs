namespace RestX.BLL.DataTranferObjects.Share;

public class SelectOption
{
    public SelectOption() { }

    public SelectOption(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}