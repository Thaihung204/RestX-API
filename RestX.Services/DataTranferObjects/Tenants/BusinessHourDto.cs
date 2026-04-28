namespace RestX.BLL.DataTranferObjects.Tenants;

public class BusinessHourDto
{
    public byte DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
