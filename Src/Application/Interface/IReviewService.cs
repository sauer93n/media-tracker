using Application.DTO;
using FluentResults;

namespace Application.Interface;

public interface IReviewService
{
    Task<Result<ReviewDTO>> CreateReviewAsync(CreateReviewRequest request);
}