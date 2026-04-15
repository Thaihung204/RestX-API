namespace RestX.BLL.DataTranferObjects.AI
{
    public class AIAnalyticsRequest
    {
        public string? FilterType { get; set; } = "week";

        //full | revenue | menu | customer | operations
        public string? AnalysisType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
