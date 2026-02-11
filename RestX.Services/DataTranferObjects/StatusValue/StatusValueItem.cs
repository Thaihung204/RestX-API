namespace RestX.BLL.DataTranferObjects.StatusValue
{
    public class StatusValueItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColorCode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
