using Application.DTO;
using FluentResults;

namespace Application.Interface;

public interface IReviewService
{
    Task<PagedResult<ReviewDTO>> GetReviewsAsync(string referenceId, int pageNumber, int pageSize);
    Task<Result<ReviewDTO>> CreateReviewAsync(CreateReviewRequest request);
    Task<Result<ReviewDTO>> UpdateReviewAsync(UpdateReviewRequest request);
    Task<Result> DeleteReviewAsync(Guid reviewId);
    Task<Result> LikeReviewAsync(Guid reviewId, Guid userId);
    Task<Result> DislikeReviewAsync(Guid reviewId, Guid userId);
}