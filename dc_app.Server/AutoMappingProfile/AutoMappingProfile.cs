
using AutoMapper;
using ServiceLibrary.Entities;

namespace dc_app.Server.AutoMappingProfile;

public class AutoMappingProfile : Profile
{
    public AutoMappingProfile()
    {
        CreateMap<SpreadsheetConfig, SpreadsheetConfigDto>()
            .ForMember(dest =>
                dest.id,
                opt => opt.MapFrom(src => src.url_id));

        CreateMap<ColumnConfig, ColumnConfigDto>()
            .ForMember(dest =>
                dest.name,
                opt => opt.MapFrom(src => src.col_name_web));
        CreateMap<UserHasSpreadsheet, UserHasSpreadsheetDto>();
    }
}
