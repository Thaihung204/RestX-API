namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class DishTrendItem
    {
        public Guid DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentQty { get; set; }
        public int PrevQty { get; set; }
        public decimal CurrentRevenue { get; set; }
        public decimal PrevRevenue { get; set; }
        public double GrowthPercent { get; set; }

        //growing | declining | stable | new
        public string Trend { get; set; } = "stable";
    }
}
