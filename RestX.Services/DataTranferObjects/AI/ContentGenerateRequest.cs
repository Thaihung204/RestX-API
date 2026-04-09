namespace RestX.BLL.DataTranferObjects.AI
{
    public class ContentGenerateRequest
    {
        public Guid? DishId { get; set; }
        public Guid? ComboId { get; set; }
        public Guid? PromotionId { get; set; }
        public string Tone { get; set; } = "friendly";
        public string? CustomContext { get; set; }
        public int Variants { get; set; } = 3;
    }
}
