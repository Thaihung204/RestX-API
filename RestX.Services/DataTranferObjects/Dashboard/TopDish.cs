namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class TopDish
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsFallback { get; set; }
        public List<DishItem> Dishes { get; set; } = new();

        public class DishItem
        {
            public Guid DishId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
        }
    }
}
