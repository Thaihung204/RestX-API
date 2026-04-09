namespace RestX.BLL.DataTranferObjects.AI
{
    public class CampaignPackResponse
    {
        public string Theme { get; set; } = string.Empty;
        public string? SpecialOccasion { get; set; }
        public CampaignChannel Facebook { get; set; } = new();
        public CampaignChannel Instagram { get; set; } = new();
        public CampaignChannel Email { get; set; } = new();
        public CampaignChannel PromotionBanner { get; set; } = new();
    }

    public class CampaignChannel
    {
        public string? Headline { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<string>? Hashtags { get; set; }
        public string? ImagePrompt { get; set; }
    }
}
