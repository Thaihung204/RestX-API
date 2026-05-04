namespace RestX.BLL.DataTranferObjects.Dashboard
{
    public class RecentFeedbackItem
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsAnonymous { get; set; }
        public string? CustomerName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class RecentFeedbacks
    {
        public List<RecentFeedbackItem> Items { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalCount { get; set; }
    }
}
