using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.DataTranferObjects.Reservation;
using System;

namespace RestX.BLL.DataTranferObjects.Table
{
    public class TableSessionInfo
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public string TableCode { get; set; }
        public Guid? OrderId { get; set; }
        public CustomerResponse Customer { get; set; }
        public Guid? ReservationId { get; set; }
        public ReservationDetail Reservation { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsActive { get; set; }

        public string? OrderReference { get; set; }
        public decimal? OrderTotalAmount { get; set; }
    }
}