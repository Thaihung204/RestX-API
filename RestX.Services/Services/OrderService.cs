using AutoMapper;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.Interfaces;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Orders;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class OrderService : BaseService, IOrderService
    {
        private readonly IMapper mapper;

        public OrderService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        public async Task<IEnumerable<DataTranferObjects.Orders.Order>> GetAllOrders()
        {
            var orders = (await Repo.GetAllAsync<Models.Orders.Order>(
                orderBy: q => q.OrderByDescending(o => o.CreatedDate),
                includeProperties: "OrderDetails,OrderTables"
            )).ToList();

            return mapper.Map<List<DataTranferObjects.Orders.Order>>(orders);
        }

        public async Task<DataTranferObjects.Orders.Order?> GetOrderById(Guid id)
        {
            var order = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == id,
                includeProperties: "OrderDetails,OrderTables"
            );

            return mapper.Map<DataTranferObjects.Orders.Order>(order);
        }

        public async Task<Guid> CreateOrder(DataTranferObjects.Orders.Order order, string userId)
        {
            var dishIds = order.OrderDetails
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            var dishes = (await Repo.GetAsync<Dish>(filter: d => dishIds.Contains(d.Id))).ToList();
            var dishesById = dishes.ToDictionary(d => d.Id, d => d);

            var discountAmount = order.DiscountAmount ?? 0m;
            var taxAmount = order.TaxAmount ?? 0m;
            var serviceCharge = order.ServiceCharge ?? 0m;

            var subTotal = order.OrderDetails.Sum(x => x.Quantity * dishesById[x.DishId].Price);
            var totalAmount = (subTotal - discountAmount) + taxAmount + serviceCharge;

            var orderEntity = new Models.Orders.Order
            {
                Reference = await GetNextOrderReference(),

                CustomerId = order.CustomerId,
                ReservationId = order.ReservationId,

                OrderStatusId = order.OrderStatusId,
                PaymentStatusId = order.PaymentStatusId,

                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                ServiceCharge = serviceCharge,

                SubTotal = subTotal,
                TotalAmount = totalAmount,

                OrderTables = new List<OrderTable>
                {
                    new OrderTable { TableId = order.TableId }
                },

                OrderDetails = order.OrderDetails.Select(d => new Models.Orders.OrderDetail
                {
                    DishId = d.DishId,
                    Quantity = d.Quantity,
                    Note = d.Note,
                    ItemStatusId = d.StatusId ?? 0
                }).ToList()
            };

            await Repo.CreateAsync(orderEntity, userId);

            return orderEntity.Id;
        }
        public async Task<Guid> UpdateOrder(Guid id, DataTranferObjects.Orders.Order orderDto, string userId)
        {
            var orderEntity = await Repo.GetOneAsync<Models.Orders.Order>(
                filter: o => o.Id == id,
                includeProperties: "OrderDetails,OrderTables"
            );

            if (orderEntity == null)
                return Guid.Empty;

            orderEntity.Reference = orderDto.Reference ?? orderEntity.Reference;
            orderEntity.CustomerId = orderDto.CustomerId;
            orderEntity.ReservationId = orderDto.ReservationId;

            orderEntity.DiscountAmount = orderDto.DiscountAmount ?? 0m;
            orderEntity.TaxAmount = orderDto.TaxAmount ?? 0m;
            orderEntity.ServiceCharge = orderDto.ServiceCharge ?? 0m;

            orderEntity.CompletedAt = orderDto.CompletedAt;
            orderEntity.CancelledAt = orderDto.CancelledAt;
            orderEntity.HandledBy = orderDto.HandledBy;

            if (orderEntity.OrderDetails?.Any() == true)
            {
                foreach (var d in orderEntity.OrderDetails.ToList())
                    Repo.Delete<Models.Orders.OrderDetail>(d.Id);
            }

            if (orderDto.OrderDetails?.Any() == true)
            {
                foreach (var d in orderDto.OrderDetails)
                {
                    await Repo.CreateAsync(new Models.Orders.OrderDetail
                    {
                        OrderId = orderEntity.Id,
                        DishId = d.DishId,
                        Quantity = d.Quantity,
                        Note = d.Note,
                        ItemStatusId = d.StatusId ?? 0
                    });
                }
            }

            if (orderEntity.OrderTables?.Any() == true)
            {
                foreach (var ot in orderEntity.OrderTables.ToList())
                    Repo.Delete<OrderTable>(ot.Id);
            }

            var tableIds = (orderDto.TableIds ?? new List<Guid>())
                .Append(orderDto.TableId)
                .Distinct()
                .ToList();

            foreach (var tableId in tableIds)
            {
                await Repo.CreateAsync(new OrderTable
                {
                    OrderId = orderEntity.Id,
                    TableId = tableId
                });
            }

            if (orderDto.OrderDetails?.Any() == true)
            {
                var dishIds = orderDto.OrderDetails.Select(x => x.DishId).Distinct().ToList();
                var dishes = (await Repo.GetAsync<Dish>(filter: d => dishIds.Contains(d.Id))).ToList();
                var dishById = dishes.ToDictionary(d => d.Id, d => d);

                orderEntity.SubTotal = orderDto.OrderDetails.Sum(x => x.Quantity * dishById[x.DishId].Price);
            }

            orderEntity.TotalAmount =
                (orderEntity.SubTotal - orderEntity.DiscountAmount) +
                orderEntity.TaxAmount +
                orderEntity.ServiceCharge;

            Repo.Update(orderEntity);
            await Repo.SaveAsync();

            return orderEntity.Id;
        }
        private async Task<string> GetNextOrderReference()
        {
            var tenantPrefix = CurrentTenant.Prefix;
            var reference = $"{tenantPrefix}{DateTime.UtcNow:yMdsff}";

            var exists = await Repo.GetExistsAsync<Models.Orders.Order>(o => o.Reference == reference);
            var count = 0;

            while (exists && count < 20)
            {
                if (count < 1)
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow:yMdsff}";
                }
                else if (count < 2)
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow:yMdsfff}";
                }
                else if (count < 10)
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow:yMdsHHfff}";
                }
                else
                {
                    reference = $"{tenantPrefix}{DateTime.UtcNow:yMdsHHmmfff}";
                }

                exists = await Repo.GetExistsAsync<Models.Orders.Order>(o => o.Reference == reference);
                count++;
            }

            return reference;
        }

        public async Task DeleteOrder(Guid id)
        {
            var order = await Repo.GetByIdAsync<Models.Orders.Order>(id);
            if (order == null)
                return;

            Repo.Delete<Models.Orders.Order>(id);
            await Repo.SaveAsync();
        }
    }
}