using Application.DTO;
using Application.Interface;
using AutoMapper;
using Domain.Entity;
using FluentResults;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Application.Service;

public class ReviewService(ReviewContext reviewContext, IEventPublisher eventPublisher, IMapper mapper) 
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

    public async Task<Result> DeleteReviewAsync(Guid reviewId)
    {
        try
        {
            var reviewEntity = await reviewContext.Reviews.FindAsync(reviewId);
            if (reviewEntity == null) return Result.Fail("Review not found");
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

    public async Task<PagedResult<ReviewDTO>> GetReviewsAsync(string referenceId, int pageNumber, int pageSize)
    {
        try
        {
            var query = reviewContext.Reviews
                .Where(r => r.ReferenceId == referenceId && !r.IsDeleted);

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
            throw new Exception("Error retrieving reviews", ex);
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

    public async Task<Result<ReviewDTO>> UpdateReviewAsync(UpdateReviewRequest request)
    {
        try
        {
            var reviewEntity = await reviewContext.Reviews.FindAsync(request.ReviewId);
            if (reviewEntity == null) return Result.Fail("Review not found");

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