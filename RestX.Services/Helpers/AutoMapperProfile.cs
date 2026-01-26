using AutoMapper;
using RestX.Models.Enum;
using RestX.Models.Menu;
using RestX.Models.Tenants;
using System.Globalization;

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
            CreateMap<Tenant, TenantItem>().ReverseMap();
        }
    }
}
