namespace RestX.BLL.DataTranferObjects.Table
{
    public class MoveTableRequest
    {
        public Guid SourceTableId { get; set; }
        public Guid TargetTableId { get; set; }
    }
}