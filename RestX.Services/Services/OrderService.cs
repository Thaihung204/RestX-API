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

        public async Task<IEnumerable<OrderItem>> GetAllOrders()
        {
            var orders = (await Repo.GetAllAsync<Order>(
                orderBy: q => q.OrderByDescending(o => o.CreatedDate),
                includeProperties: "OrderDetails,OrderTables"
            )).ToList();

            return mapper.Map<List<OrderItem>>(orders);
        }

        public async Task<OrderItem?> GetOrderById(Guid id)
        {
            var order = await Repo.GetOneAsync<Order>(
                filter: o => o.Id == id,
                includeProperties: "OrderDetails,OrderTables"
            );

            return mapper.Map<OrderItem>(order);
        }

        public async Task<Guid> UpsertOrder(OrderItem orderItem)
        {
            if (orderItem.Id.HasValue && orderItem.Id.Value != Guid.Empty)
            {
                var order = await Repo.GetOneAsync<Order>(
                    filter: o => o.Id == orderItem.Id.Value,
                    includeProperties: "OrderDetails,OrderTables"
                );

                if (order == null)
                    return Guid.Empty;

                order.Reference = orderItem.Reference ?? order.Reference;
                order.CustomerId = orderItem.CustomerId;
                order.ReservationId = orderItem.ReservationId;
                order.OrderStatusId = orderItem.OrderStatusId;
                order.PaymentStatusId = orderItem.PaymentStatusId;
                order.SubTotal = orderItem.SubTotal;
                order.DiscountAmount = orderItem.DiscountAmount;
                order.TaxAmount = orderItem.TaxAmount;
                order.ServiceCharge = orderItem.ServiceCharge;
                order.TotalAmount = orderItem.TotalAmount;
                order.CompletedAt = orderItem.CompletedAt;
                order.CancelledAt = orderItem.CancelledAt;
                order.HandledBy = orderItem.HandledBy;

                if (order.OrderDetails?.Any() == true)
                {
                    foreach (var d in order.OrderDetails.ToList())
                        Repo.Delete<OrderDetail>(d.Id);
                }

                if (orderItem.OrderDetails?.Any() == true)
                {
                    foreach (var d in orderItem.OrderDetails)
                    {
                        var detail = new OrderDetail
                        {
                            OrderId = order.Id,
                            DishId = d.DishId,
                            Quantity = d.Quantity,
                            Note = d.Note,
                            ItemStatusId = d.ItemStatusId
                        };
                        await Repo.CreateAsync(detail);
                    }
                }

                if (order.OrderTables?.Any() == true)
                {
                    foreach (var ot in order.OrderTables.ToList())
                        Repo.Delete<OrderTable>(ot.Id);
                }

                if (orderItem.TableIds?.Any() == true)
                {
                    foreach (var tableId in orderItem.TableIds.Distinct())
                    {
                        await Repo.CreateAsync(new OrderTable
                        {
                            OrderId = order.Id,
                            TableId = tableId
                        });
                    }
                }

                Repo.Update(order);
                await Repo.SaveAsync();
                return order.Id;
            }
            else
            {
                var order = mapper.Map<Order>(orderItem);

                order.Reference = string.IsNullOrWhiteSpace(orderItem.Reference)
                    ? $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}"
                    : orderItem.Reference;

                await Repo.CreateAsync(order);
                await Repo.SaveAsync();

                if (orderItem.OrderDetails?.Any() == true)
                {
                    foreach (var d in orderItem.OrderDetails)
                    {
                        await Repo.CreateAsync(new OrderDetail
                        {
                            OrderId = order.Id,
                            DishId = d.DishId,
                            Quantity = d.Quantity,
                            Note = d.Note,
                            ItemStatusId = d.ItemStatusId
                        });
                    }
                }

                if (orderItem.TableIds?.Any() == true)
                {
                    foreach (var tableId in orderItem.TableIds.Distinct())
                    {
                        await Repo.CreateAsync(new OrderTable
                        {
                            OrderId = order.Id,
                            TableId = tableId
                        });
                    }
                }

                await Repo.SaveAsync();
                return order.Id;
            }
        }

        public async Task DeleteOrder(Guid id)
        {
            var order = await Repo.GetByIdAsync<Order>(id);
            if (order == null)
                return;

            Repo.Delete<Order>(id);
            await Repo.SaveAsync();
        }
        public async Task<OrderItem> CreateOrder(OrderItem orderItem)
        {
            var activeOrderTable = await Repo.GetOneAsync<OrderTable>(
                filter: ot =>
                    ot.TableId == orderItem.TableId &&
                    (ot.Order.OrderStatusId == OrderStatus.Serving),
                includeProperties: "Order"
            );

            var dishes = (await Repo.GetAsync<Dish>(filter: d => orderItem.OrderDetails.Select(x => x.DishId).Distinct().ToList().Contains(d.Id))).ToList();
            var dishById = dishes.ToDictionary(d => d.Id, d => d);

            var order = new Order
            {
                Reference = string.IsNullOrWhiteSpace(orderItem.Reference)
                    ? $"ord{(await Repo.GetCountAsync<Order>(o => o.CreatedDate >= DateTime.UtcNow.Date && o.CreatedDate < DateTime.UtcNow.Date.AddDays(1))) + 1}"
                    : orderItem.Reference,
                CustomerId = orderItem.CustomerId,
                ReservationId = orderItem.ReservationId,
                OrderStatusId = orderItem.OrderStatusId == default ? OrderStatus.Reserved : orderItem.OrderStatusId,
                PaymentStatusId = orderItem.PaymentStatusId,
                DiscountAmount = orderItem.DiscountAmount,
                TaxAmount = orderItem.TaxAmount,
                ServiceCharge = orderItem.ServiceCharge,
                SubTotal = 0,
                TotalAmount = 0,
                HandledBy = orderItem.HandledBy
            };

            await Repo.CreateAsync(order);
            await Repo.CreateAsync(new OrderTable
            {
                OrderId = order.Id,
                TableId = orderItem.TableId
            });

            foreach (var orderDetails in orderItem.OrderDetails)
            {
                await Repo.CreateAsync(new OrderDetail
                {
                    OrderId = order.Id,
                    DishId = orderDetails.DishId,
                    Quantity = orderDetails.Quantity,
                    Note = orderDetails.Note,
                    ItemStatusId = orderDetails.ItemStatusId
                });
            }

            order.SubTotal = orderItem.OrderDetails.Sum(x => x.Quantity * dishById[x.DishId].Price);
            order.TotalAmount = order.SubTotal - order.DiscountAmount + order.TaxAmount + order.ServiceCharge;

            Repo.Update(order);
            await Repo.SaveAsync();

            orderItem.Id = order.Id;
            orderItem.Reference = order.Reference;
            orderItem.SubTotal = order.SubTotal;
            orderItem.TotalAmount = order.TotalAmount;

            if (orderItem.TableIds == null) orderItem.TableIds = new List<Guid>();
            if (!orderItem.TableIds.Contains(orderItem.TableId)) orderItem.TableIds.Add(orderItem.TableId);

            return orderItem;
        }
    }
}