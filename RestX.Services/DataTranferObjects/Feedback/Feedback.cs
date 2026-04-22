using Microsoft.AspNetCore.Http;
using RestX.BLL.DataTranferObjects.Common;
using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Feedback
{
    public class FeedbackFilterParams : BaseFilterParams
    {
        public Guid? OrderId { get; set; }
        public Guid? CustomerId { get; set; }
        public bool? IsPublished { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
    }

    public class FeedbackCreate
    {
        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public bool IsAnonymous { get; set; } = false;

        public IFormFile[]? Images { get; set; }
    }

    public class FeedbackUpdate
    {
        [Range(1, 5)]
        public int? Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public bool? IsAnonymous { get; set; }

        // Admin only
        public bool? IsPublished { get; set; }

        public Guid[]? RemoveImageIds { get; set; }
        public IFormFile[]? NewImages { get; set; }
    }

    public class FeedbackCustomerInfo
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public class FeedbackImageItem
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsCover { get; set; }
    }

    public class FeedbackItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsPublished { get; set; }
        public bool IsAnonymous { get; set; }
        public DateTime CreatedDate { get; set; }
        public FeedbackCustomerInfo? Customer { get; set; }
        public IEnumerable<FeedbackImageItem> FeedbackImages { get; set; } = new List<FeedbackImageItem>();
    }
}