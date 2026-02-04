using AutoMapper;
using RestX.BLL.DataTranferObjects.Category;
using RestX.BLL.DataTranferObjects.Table;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using System.Globalization;
using RestX.Models.Tables;

namespace RestX.BLL.Helpers
{
    public class AutoMapperProfile : Profile
    {
        private TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
        public AutoMapperProfile()
        {
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
