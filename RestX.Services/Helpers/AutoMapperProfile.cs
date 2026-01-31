using AutoMapper;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using System.Globalization;
using RestX.BLL.DataTranferObjects.Auth;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Employee;
using RestX.Models.Customers;
using RestX.Models.HR;
using RestX.Models.Identity;
using RestX.BLL.DataTranferObjects.Table;
using RestX.Models.Tables;

namespace RestX.BLL.Helpers
{
    public class AutoMapperProfile : Profile
    {
        private TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
        public AutoMapperProfile()
        {
            // Auth mappings
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
                .ForMember(dest => dest.TotalOrders, opt => opt.Ignore())
                .ForMember(dest => dest.TotalReservations, opt => opt.Ignore());
            CreateMap<Employee, EmployeeResponse>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
            CreateMap<Table, TableResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.TableStatus));
            CreateMap<TableStatus, TableStatus>();
            // CreateMap<Source, Destination>();
            CreateMap<Dish, DishItem>()
                .ForMember(
                    dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(
                    dest => dest.MainImageUrl,
                    opt => opt.MapFrom(src =>
                        src.DishImages
                            .Where(x => x.IsActive && x.ImageType == DishImageType.Main)
                            .OrderBy(x => x.DisplayOrder)
                            .ThenBy(x => x.Id)
                            .Select(x => x.ImageUrl)
                            .FirstOrDefault()));
            this.CreateMap<Tenant, TenantOverview>().ReverseMap();
        }
    }
}
