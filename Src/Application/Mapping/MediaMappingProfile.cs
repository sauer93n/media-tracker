using Application.DTO;
using AutoMapper;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace Application.Mapping;

public class MediaMappingProfile : Profile
{
    public MediaMappingProfile()
    {
        CreateMap<Movie, MediaDetailsDTO>()
            .ForMember(dest => dest.Genre, opt => opt.MapFrom(src => src.Genres.FirstOrDefault() != null ? src.Genres.FirstOrDefault().Name : ""))
            .ForMember(dest => dest.Cast, opt => opt.MapFrom(src => src.Credits.Cast.Select(c => c.Name).ToList()))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate ?? DateTime.MinValue))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Overview));

        CreateMap<TvShow, MediaDetailsDTO>()
            .ForMember(dest => dest.Genre, opt => opt.MapFrom(src => src.Genres.FirstOrDefault() != null ? src.Genres.FirstOrDefault().Name : ""))
            .ForMember(dest => dest.Cast, opt => opt.MapFrom(src => src.Credits.Cast.Select(c => c.Name).ToList()))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.FirstAirDate ?? DateTime.MinValue))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Overview));

        CreateMap<SearchMovie, MediaDetailsDTO>()
            .ForMember(dest => dest.Genre, opt => opt.Ignore())
            .ForMember(dest => dest.Cast, opt => opt.Ignore())
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate ?? DateTime.MinValue))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Overview));

        CreateMap<SearchTv, MediaDetailsDTO>()
            .ForMember(dest => dest.Genre, opt => opt.Ignore())
            .ForMember(dest => dest.Cast, opt => opt.Ignore())
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.FirstAirDate ?? DateTime.MinValue))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Overview));
    }
}