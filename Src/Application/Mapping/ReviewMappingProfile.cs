using Application.DTO;
using AutoMapper;

namespace Application.Mapping;

public class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        CreateMap<Domain.Entity.Review, ReviewDTO>()
            .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.Author.Id))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId));

        CreateMap<CreateReviewRequest, Domain.Entity.Review>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId));

        CreateMap<Domain.Entity.Review, Infrastructure.Entity.Review>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.Author.Id))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.Likes, opt => opt.MapFrom(src => src.Likes))
            .ForMember(dest => dest.Dislikes, opt => opt.MapFrom(src => src.Dislikes))
            .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId));

        CreateMap<Infrastructure.Entity.Review, Domain.Entity.Review>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => new Domain.Entity.User(src.AuthorId)))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.Likes, opt => opt.MapFrom(src => src.Likes))
            .ForMember(dest => dest.Dislikes, opt => opt.MapFrom(src => src.Dislikes))
            .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId));
    }
}