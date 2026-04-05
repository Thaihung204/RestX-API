using AutoMapper;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.DataTranferObjects.Category;
using RestX.BLL.DataTranferObjects.Combo;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.DataTranferObjects.Employee;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.DataTranferObjects.Payments;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.DataTranferObjects.Status;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.Models.Common;
using RestX.Models.Customers;
using RestX.Models.Enum;
using RestX.Models.HR;
using RestX.Models.Identity;
using RestX.Models.Inventory;
using RestX.Models.Menu;
using RestX.Models.Reservations;
using RestX.Models.Tables;
using RestX.Models.Tenants;
using RestX.Models.Triggers;
using System.Globalization;
using FloorEntity = RestX.Models.Tables.Floor;
using LoyaltyPointBandEntity = RestX.Models.Loyalty.LoyaltyPointBand;

namespace RestX.BLL.Helpers
{
    public class AutoMapperProfile : Profile
    {
        private TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
        public AutoMapperProfile()
        {
            CreateMap<ApplicationUser, UserInfo>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
            CreateMap<Customer, CustomerResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.ApplicationUser.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ApplicationUser.Email ?? string.Empty))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.ApplicationUser.FullName ?? string.Empty))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.ApplicationUser.PhoneNumber))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.ApplicationUser.AvatarUrl))
                .ForMember(dest => dest.TotalOrders, opt => opt.Ignore())
                .ForMember(dest => dest.TotalReservations, opt => opt.Ignore());
            CreateMap<Employee, EmployeeResponse>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            CreateMap<Dish, DishItem>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.DishImages));
            CreateMap<DishItem, Dish>()
                .ForMember(dest => dest.DishImages, opt => opt.Ignore());
            CreateMap<DishImage, DishImageItem>()
                .ForMember(dest => dest.File, opt => opt.Ignore());
            CreateMap<DishImageItem, DishImage>()
                .ForMember(dest => dest.Dish, opt => opt.Ignore());

            CreateMap<Dish, MenuItem>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src =>
                    src.DishImages.OrderByDescending(img => img.ImageType == DishImageType.Main)
                                 .ThenBy(img => img.DisplayOrder)
                                 .Select(img => img.ImageUrl)
                                 .FirstOrDefault()));
            CreateMap<Category, MenuCategory>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Dishes));

            this.CreateMap<Tenant, TenantOverview>().ReverseMap();
            CreateMap<Tenant, TenantItem>().ReverseMap();
            CreateMap<Category, CategoryItem>()
                .ForMember(
                    dest => dest.CategoryChildrens,
                    opt => opt.MapFrom(src =>
                        src.SubCategories
                            .Where(c => c.IsActive)
                    )
                );
            CreateMap<Table, TableItem>()
                .ForMember(
                    dest => dest.TableStatusName,
                    opt => opt.MapFrom(src => src.TableStatusId.ToString())
                )
                .ForMember(
                    dest => dest.FloorName,
                    opt => opt.MapFrom(src => src.Floor != null ? src.Floor.Name : string.Empty)
                );
            CreateMap<TableItem, Table>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Floor, opt => opt.Ignore())
                .ForMember(dest => dest.Table3DModel, opt => opt.Ignore());
            CreateMap<Models.Tenants.TenantRequest, DataTranferObjects.Tenants.TenantRequest>().ReverseMap();
            CreateMap<Models.Tenants.TenantRequest, TenantItem>().ReverseMap();
            CreateMap<FloorEntity, DataTranferObjects.Floor.Floor>()
                .ForMember(dest => dest.Image, opt => opt.Ignore())
                .ForMember(dest => dest.TableCount, opt => opt.MapFrom(src => src.Tables != null ? src.Tables.Count : 0));
            CreateMap<DataTranferObjects.Floor.Floor, FloorEntity>()
                .ForMember(dest => dest.Tables, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());
            CreateMap<Ingredient, IngredientItem>()
                .ForMember(dest => dest.CurrentQuantity, opt => opt.MapFrom(src =>
                    src.InventoryStock != null ? src.InventoryStock.CurrentQuantity : 0))
                .ReverseMap()
                .ForMember(dest => dest.InventoryStock, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore());
            CreateMap<Supplier, SupplierItem>().ReverseMap();
            CreateMap<Models.Orders.OrderDetail, DataTranferObjects.Orders.OrderDetail>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.ItemStatus != null ? src.ItemStatus.Name : null));

            CreateMap<DataTranferObjects.Orders.OrderDetail, Models.Orders.OrderDetail>()
                .ForMember(dest => dest.ItemStatusId, opt => opt.Ignore())
                .ForMember(dest => dest.ItemStatus, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.Dish, opt => opt.Ignore()); CreateMap<Models.Orders.Order, DataTranferObjects.Orders.Order>();

            CreateMap<DataTranferObjects.Orders.Order, Models.Orders.Order>()
                .ForMember(dest => dest.OrderDetails, opt => opt.Ignore()); CreateMap<Models.Inventory.IngredientCategory, DataTranferObjects.Inventory.IngredientCategory>().ReverseMap();

            CreateMap<LoyaltyPointBandEntity, DataTranferObjects.Loyalty.LoyaltyPointBand>().ReverseMap();
            CreateMap<Models.Orders.Payment, PaymentDetail>();

            CreateMap<StatusValue, StatusValues>();
            CreateMap<StatusValues, StatusValue>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StatusTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.StatusType, opt => opt.Ignore());
            CreateMap<StatusValue, ReservationStatusInfo>();

            CreateMap<Reservation, ReservationContactInfo>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Customer.ApplicationUser.FullName ?? string.Empty))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Customer.ApplicationUser.PhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Customer.ApplicationUser.Email))
                .ForMember(dest => dest.MembershipLevel, opt => opt.MapFrom(src => src.Customer.MembershipLevel))
                .ForMember(dest => dest.LoyaltyPoints, opt => opt.MapFrom(src => src.Customer.LoyaltyPoints));

            CreateMap<Reservation, ReservationListItem>()
                .ForMember(dest => dest.ReservationDateTime, opt => opt.MapFrom(src => src.Time))
                .ForMember(dest => dest.ContactName, opt => opt.MapFrom(src => src.Customer.ApplicationUser.FullName ?? string.Empty))
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.Customer.ApplicationUser.PhoneNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.ReservationStatus))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate));

            CreateMap<Reservation, ReservationDetail>()
                .ForMember(dest => dest.ReservationDateTime, opt => opt.MapFrom(src => src.Time))
                .ForMember(dest => dest.Contact, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.ReservationStatus))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModifiedDate));

            CreateMap<DishRecipe, DishRecipeItem>().ReverseMap();
            CreateMap<MealCombo, ComboSummary>().ReverseMap();
            CreateMap<ComboDetail, ComboDetailItem>().ReverseMap();
            CreateMap<Models.Promotions.Promotion, BLL.DataTranferObjects.Promotion.Promotion>().ReverseMap();

            CreateMap<TriggerObject, DataTransferObjects.Triggers.TriggerObject>().ReverseMap();
            CreateMap<RestX.Models.Triggers.Trigger, RestX.BLL.DataTransferObjects.Triggers.Trigger>().ReverseMap();
            CreateMap<RestX.Models.Triggers.TriggerAction, RestX.BLL.DataTransferObjects.Triggers.TriggerAction>()
                .ForMember(dest => dest.CustomProperties, opt => opt.Ignore())
                .ForMember(dest => dest.Groups, opt => opt.Ignore()).ReverseMap();
            CreateMap<RestX.Models.Triggers.TriggerCriteria, RestX.BLL.DataTransferObjects.Triggers.TriggerCriteria>().ReverseMap();
            CreateMap<RestX.Models.Triggers.TriggerGroup, RestX.BLL.DataTransferObjects.Triggers.TriggerGroup>();
            CreateMap<Models.Reservations.TableSession, TableSessionInfo>()
                .ForMember(dest => dest.OrderReference, opt => opt.MapFrom(src => src.Order != null ? src.Order.Reference : null))
                .ForMember(dest => dest.OrderTotalAmount, opt => opt.MapFrom(src => src.Order != null ? src.Order.TotalAmount : (decimal?)null));

        }
    }
}
