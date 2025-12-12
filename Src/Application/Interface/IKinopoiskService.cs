using Application.DTO;
using Domain.Entity;
using FluentResults;

namespace Application.Interface;

public interface IKinopoiskService
{
    Task<Result<IEnumerable<KinopoiskRatingDTO>>> ImportUserRatings(string userId);
    Task<Result<MediaDTO>> FindMediaByKinopoiskId(int kinopoiskId);
    Task<Result<IEnumerable<ReviewDTO>>> ConvertRatingsToReviews(IEnumerable<KinopoiskRatingDTO> ratings, User user);
}