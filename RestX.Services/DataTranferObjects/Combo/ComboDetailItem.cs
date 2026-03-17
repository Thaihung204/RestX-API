namespace RestX.BLL.DataTranferObjects.Combo
{
    public class ComboDetailItem
    {
        public Guid? Id { get; set; }
        public Guid DishId { get; set; }
        public string? DishName { get; set; }
        public decimal? DishPrice { get; set; }
        public int Quantity { get; set; } = 1;
    }
}