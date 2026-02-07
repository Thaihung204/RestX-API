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
                .ForMember(dest => dest.TotalOrders, opt => opt.Ignore())
                .ForMember(dest => dest.TotalReservations, opt => opt.Ignore());
            CreateMap<Employee, EmployeeResponse>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            // CreateMap<Source, Destination>();
            CreateMap<Dish, DishItem>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src =>
                    src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.MainImageUrl,
                opt => opt.MapFrom(src =>
                    src.DishImages
                        .Where(x => x.ImageType == DishImageType.Main && x.IsActive)
                        .OrderByDescending(x => x.CreatedDate)
                        .Select(x => x.ImageUrl)
                        .FirstOrDefault()
                ))
            .ForMember(dest => dest.SubImages,
                opt => opt.MapFrom(src =>
                    src.DishImages
                        .Where(x => x.ImageType == DishImageType.Sub && x.IsActive)
                        .OrderBy(x => x.DisplayOrder)
                        .Select(x => new DishImageItem
                        {
                            Id = x.Id,
                            DisplayOrder = x.DisplayOrder,
                            ImageUrl = x.ImageUrl
                        })
                        .ToList()
                ));
            CreateMap<Dish, MenuItem>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty)
            )
            .ForMember(
                dest => dest.ImageUrl,
                opt => opt.MapFrom(src =>
                    src.DishImages
                        .Where(i =>
                            i.IsActive &&
                            i.ImageType == DishImageType.Main
                        )
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                )
            );
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
                    opt => opt.MapFrom(src => src.TableStatus != null ? src.TableStatus.Name : null)
                );
            CreateMap<TableItem, Table>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TableStatus, opt => opt.Ignore())
                .ForMember(dest => dest.Table3DModel, opt => opt.Ignore());
        }
    }
}
