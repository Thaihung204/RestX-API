using System.ComponentModel.DataAnnotations;

namespace RestX.BLL.DataTranferObjects.Table
{
    public class MergeTableRequest
    {
        [Required]
        public List<Guid> TableIds { get; set; } = new List<Guid>();

        public Guid? ReservationId { get; set; }
        public Guid? CustomerId { get; set; }
    }

    public class MergeTableResponse
    {
        public Guid? OrderId { get; set; }
        public bool RequiresManualResolution { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<Guid> ExistingOrderIds { get; set; } = new List<Guid>();
        public List<TableSessionInfo> Sessions { get; set; } = new List<TableSessionInfo>();
    }
}