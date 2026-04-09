namespace RestX.BLL.DataTranferObjects.AI
{
    public class CampaignPackRequest
    {
        public string Theme { get; set; } = string.Empty;
        public string Tone { get; set; } = "friendly";
        public string? PromotionDetail { get; set; }
        public string? CustomContext { get; set; }
    }
}
