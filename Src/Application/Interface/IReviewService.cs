using Application.DTO;
using Domain.Entity;
using FluentResults;

namespace Application.Interface;

public interface IReviewService
{
    Task<Result<ReviewDTO>> GetReviewByIdAsync(User domainUser, Guid reviewId);
    Task<Result<PagedResult<ReviewDTO>>> GetUserReviewsAsync(User domainUser, ReferenceType referenceType, int pageNumber, int pageSize);
    Task<Result<PagedResult<ReviewDTO>>> GetReviewsAsync(User domainUser, int pageNumber, int pageSize);
    Task<Result<PagedResult<ReviewDTO>>> GetReviewsForTypeAsync(User domainUser, ReferenceType referenceType, int pageNumber, int pageSize);
    Task<Result<ReviewDTO>> CreateReviewAsync(CreateReviewRequest request);
    Task<Result<ReviewDTO>> UpdateReviewAsync(Guid updater, UpdateReviewRequest request);
    Task<Result> DeleteReviewAsync(Guid deleter, Guid reviewId);
    Task<Result> LikeReviewAsync(Guid reviewId, Guid userId);
    Task<Result> DislikeReviewAsync(Guid reviewId, Guid userId);
}