using AutoMapper;
using RestX.BLL.DataTranferObjects.Category;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using System.Globalization;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Employee;
using RestX.Models.Customers;
using RestX.Models.HR;
using RestX.Models.Identity;
using RestX.Models.Tables;
using RestX.BLL.DataTranferObjects.Floor;
using FloorEntity = RestX.Models.Tables.Floor;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.Models.Inventory;
using LoyaltyPointBandEntity = RestX.Models.Loyalty.LoyaltyPointBand;
using RestX.BLL.DataTranferObjects.Status;
using RestX.Models.Common;

namespace RestX.BLL.Helpers
{
    public class AutoMapperProfile : Profile
    {
        private TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
        public AutoMapperProfile()
        {
            CreateMap<ApplicationUser, UserInfo>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.UserName ?? string.Empty))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
            CreateMap<Customer, CustomerResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.ApplicationUser.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ApplicationUser.Email ?? string.Empty))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.ApplicationUser.UserName ?? string.Empty))
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

            CreateMap<Models.Inventory.IngredientCategory, DataTranferObjects.Inventory.IngredientCategory>().ReverseMap();

            CreateMap<LoyaltyPointBandEntity, DataTranferObjects.Loyalty.LoyaltyPointBand>().ReverseMap();
            CreateMap<StatusValue, StatusValues>();
            CreateMap<StatusValues, StatusValue>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StatusTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.StatusType, opt => opt.Ignore());
        }
    }
}
