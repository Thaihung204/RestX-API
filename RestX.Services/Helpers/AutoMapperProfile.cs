using System.Globalization;
using AutoMapper;

namespace RestX.BLL.Helpers
{
    public class AutoMapperProfile : Profile
    {
        private TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

        public AutoMapperProfile()
        {
            // CreateMap<Source, Destination>();
        }
    }
}
