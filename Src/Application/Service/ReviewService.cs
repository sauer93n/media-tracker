using Application.DTO;
using Application.Interface;
using AutoMapper;
using Domain.Entity;
using FluentResults;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Application.Service;

public class ReviewService(
    ReviewContext reviewContext,
    IEventPublisher eventPublisher,
    IMapper mapper) 
    : DomainService(eventPublisher), IReviewService
{
    public async Task<Result<ReviewDTO>> CreateReviewAsync(CreateReviewRequest request)
    {
        try
        {
            // Create a user entity from the request (populated by middleware)
            var user = new User(request.AuthorId, request.AuthorName);
            
            // Create the review using the factory method (generates ReviewCreatedEvent)
            var domainReview = Review.Create(
                user,
                request.Content,
                request.Rating,
                request.ReferenceId
            );
            
            // Map to infrastructure entity for persistence
            var reviewEntity = mapper.Map<Infrastructure.Entity.Review>(domainReview);

            await reviewContext.Reviews.AddAsync(reviewEntity);
            await reviewContext.SaveChangesAsync();

            // Publish domain events
            await PublishEventAsync(domainReview);

            // Map to DTO for response
            var reviewDto = mapper.Map<ReviewDTO>(domainReview);

            return Result.Ok(reviewDto);
        }
        catch (Exception ex)
        {
            return Result.Fail<ReviewDTO>(ex.Message);
        }
    }

    public async Task<Result> DeleteReviewAsync(Guid deleter, Guid reviewId)
    {
        try
        {
            var reviewEntity = await reviewContext.Reviews.FindAsync(reviewId);
            if (reviewEntity == null) return Result.Fail("Review not found");

            if (deleter.ToString() != reviewEntity.AuthorId.ToString()) 
                return Result.Fail(new Error("Unauthorized to delete this review").CausedBy(new UnauthorizedAccessException("Unauthorized to delete this review")));

            reviewContext.Reviews.Remove(reviewEntity);
            await reviewContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message); 
        }
    }

    public async Task<Result> DislikeReviewAsync(Guid reviewId, Guid userId)
    {
        try
        {
            var reviewEntity = await reviewContext.Reviews.FindAsync(reviewId);
            if (reviewEntity == null) return Result.Fail("Review not found");
            // Increment likes
            var domainReview = mapper.Map<Review>(reviewEntity);
            var dislike = await reviewContext.Dislikes.FindAsync(reviewId, userId);

            if (dislike != null)
            {
                reviewContext.Dislikes.Remove(dislike);
                await reviewContext.SaveChangesAsync();
                return Result.Ok();
            }

            domainReview.AddDislike(userId);
            await reviewContext.Dislikes.AddAsync(new Infrastructure.Entity.Dislike
            {
                ReviewId = reviewId,
                UserId = userId
            });
            await reviewContext.SaveChangesAsync();

            // Optionally, publish an event for the like action
            await PublishEventAsync(domainReview);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result<PagedResult<ReviewDTO>>> GetUserReviewsAsync(Guid userId, int pageNumber, int pageSize)
    {
        try
        {
            var query = reviewContext.Reviews
                .Where(r => r.AuthorId == userId && !r.IsDeleted)
                .Include(r => r.Likes)
                .Include(r => r.Dislikes);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var domainReviews = reviews.Select(mapper.Map<Review>).ToList();
            var reviewDtos = domainReviews.Select(mapper.Map<ReviewDTO>).ToList();

            var pagedResult = new PagedResult<ReviewDTO>
            {
                Data = reviewDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return pagedResult;
        }
        catch (Exception ex)
        {
            return Result.Fail<PagedResult<ReviewDTO>>($"Error retrieving user reviews: {ex.Message}");
        }
    }

    public async Task<Result<ReviewDTO>> GetReviewByIdAsync(Guid reviewId)
    {
        try
        {
            var reviewEntity = await reviewContext.Reviews
                .Include(r => r.Likes)
                .Include(r => r.Dislikes)
                .SingleOrDefaultAsync(r => r.Id == reviewId);
            if (reviewEntity == null) return Result.Fail<ReviewDTO>("Review not found");

            var domainReview = mapper.Map<Review>(reviewEntity);
            var reviewDto = mapper.Map<ReviewDTO>(domainReview);

            return Result.Ok(reviewDto);
        }
        catch (Exception ex)
        {
            return Result.Fail<ReviewDTO>($"Error retrieving review: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<ReviewDTO>>> GetReviewsAsync(int pageNumber, int pageSize)
    {
        try
        {
            var query = reviewContext.Reviews
                .Where(r => !r.IsDeleted)
                .Include(r => r.Likes)
                .Include(r => r.Dislikes);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var domainReviews = reviews.Select(mapper.Map<Review>).ToList();
            var reviewDtos = domainReviews.Select(mapper.Map<ReviewDTO>).ToList();

            var pagedResult = new PagedResult<ReviewDTO>
            {
                Data = reviewDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return pagedResult;
        }
        catch (Exception ex)
        {
            return Result.Fail<PagedResult<ReviewDTO>>($"Error retrieving reviews: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<ReviewDTO>>> GetReviewsForTypeAsync(ReferenceType referenceType, int pageNumber, int pageSize)
    {
        try 
        {
            var query = reviewContext.Reviews
                .Where(r => r.ReferenceType.ToString() == referenceType.ToString() && !r.IsDeleted)
                .Include(r => r.Likes)
                .Include(r => r.Dislikes);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var domainReviews = reviews.Select(mapper.Map<Review>).ToList();
            var reviewDtos = domainReviews.Select(mapper.Map<ReviewDTO>).ToList();

            var pagedResult = new PagedResult<ReviewDTO>
            {
                Data = reviewDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return pagedResult;
        }
        catch (Exception ex)
        {
            return Result.Fail<PagedResult<ReviewDTO>>($"Error retrieving reviews for type: {ex.Message}");
        }
    }

    public async Task<Result> LikeReviewAsync(Guid reviewId, Guid userId)
    {
        try
        {
            var reviewEntity = await reviewContext.Reviews.FindAsync(reviewId);
            if (reviewEntity == null) return Result.Fail("Review not found");
            // Increment likes
            var domainReview = mapper.Map<Review>(reviewEntity);
            var like = await reviewContext.Likes.FindAsync(reviewId, userId);

            if (like != null)
            {
                reviewContext.Likes.Remove(like);
                await reviewContext.SaveChangesAsync();
                return Result.Ok();
            }

            domainReview.AddLike(userId);
            await reviewContext.Likes.AddAsync(new Infrastructure.Entity.Like
            {
                ReviewId = reviewId,
                UserId = userId
            });
            await reviewContext.SaveChangesAsync();

            // Optionally, publish an event for the like action
            await PublishEventAsync(domainReview);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result<ReviewDTO>> UpdateReviewAsync(Guid updater, UpdateReviewRequest request)
    {
        try
        {
            var reviewEntity = await reviewContext.Reviews.FindAsync(request.ReviewId);
            if (reviewEntity == null) return Result.Fail("Review not found");

            if (updater.ToString() != reviewEntity.AuthorId.ToString())
                return Result.Fail(new Error("Unauthorized to update this review").CausedBy(new UnauthorizedAccessException("Unauthorized to update this review")));

            // Map updated properties
            var domainReview = mapper.Map<Review>(reviewEntity);
            domainReview.UpdateContent(request.Content);
            domainReview.UpdateRating(request.Rating);

            await reviewContext.SaveChangesAsync();

            await PublishEventAsync(domainReview);

            // Map to DTO for response
            var reviewDto = mapper.Map<ReviewDTO>(domainReview);

            return Result.Ok(reviewDto);
        }
        catch (Exception ex)
        {
            return Result.Fail<ReviewDTO>(ex.Message);
        }
    }
}