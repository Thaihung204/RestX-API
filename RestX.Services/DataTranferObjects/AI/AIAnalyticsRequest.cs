namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIAnalyticsRequest
    {
        public string? FilterType { get; set; } = "month";
        public string? AnalysisType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
