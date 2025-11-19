using Application.DTO;
using Application.Interface;
using Domain.Entity;
using FluentResults;
using Infrastructure.Context;

namespace Application.Service;

public class ReviewService(ReviewContext reviewContext, IEventPublisher eventPublisher) 
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
            var reviewEntity = new Infrastructure.Entity.Review
            {
                Id = domainReview.Id,
                AuthorId = domainReview.Author.Id,
                Content = domainReview.Content,
                Rating = domainReview.Rating,
                Likes = domainReview.Likes,
                Dislikes = domainReview.Dislikes,
                ReferenceId = domainReview.ReferenceId
            };

            await reviewContext.Reviews.AddAsync(reviewEntity);
            await reviewContext.SaveChangesAsync();

            // Publish domain events
            await PublishEventAsync(domainReview);

            // Map to DTO for response
            var reviewDto = new ReviewDTO
            {
                AuthorId = reviewEntity.AuthorId,
                Content = reviewEntity.Content,
                Rating = reviewEntity.Rating,
                ReferenceId = reviewEntity.ReferenceId
            };

            return Result.Ok(reviewDto);
        }
        catch (Exception ex)
        {
            return Result.Fail<ReviewDTO>(ex.Message);
        }
    }
}