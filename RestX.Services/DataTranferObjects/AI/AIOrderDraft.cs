namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIOrderDraft
    {
        public Guid? TableId { get; set; }
        public List<AIOrderDraftItem> Items { get; set; } = new();
        public decimal TotalEstimate => Items.Sum(i => i.Price * i.Quantity);
    }

    public class AIOrderDraftItem
    {
        public Guid DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal Price { get; set; }
    }
}
