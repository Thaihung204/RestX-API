namespace RestX.BLL.DataTranferObjects.AI
{
    public class ContentGenerateResponse
    {
        public List<ContentVariant> Variants { get; set; } = new();
    }

    public class ContentVariant
    {
        public string Headline { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
