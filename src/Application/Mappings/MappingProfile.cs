using AutoMapper;
using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Application.Interfaces;
using AppEntity = BioLicense_Portal.Domain.Entities.Application;

namespace BioLicense_Portal.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AppEntity, AppResponseDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.ApplicationType))
                .ForMember(dest => dest.Features, opt => opt.MapFrom(src => src.Features));

            CreateMap<ApplicationFeature, FeatureResponseDto>();
            
            CreateMap<User, AuthResponseDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
        }
    }
}
