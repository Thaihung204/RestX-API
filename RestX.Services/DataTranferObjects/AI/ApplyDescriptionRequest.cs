namespace RestX.BLL.DataTranferObjects.AI
{
    public class ApplyDescriptionRequest
    {
        public Guid? DishId { get; set; }
        public Guid? ComboId { get; set; }
        public Guid? PromotionId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
