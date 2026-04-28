namespace RestX.BLL.DataTranferObjects.AI
{
    public class ContentGenerateRequest
    {
        // Dish
        public string? DishName { get; set; }

        // Combo
        public string? ComboName { get; set; }
        public List<string>? ComboDishes { get; set; }

        // Promotion
        public string? PromotionName { get; set; }
        public decimal? DiscountValue { get; set; }
    }
}
