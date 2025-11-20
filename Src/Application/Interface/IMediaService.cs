using Application.DTO;
using FluentResults;

namespace Application.Interface;

public interface IMediaService
{
    Task<Result<MediaDetailsDTO>> GetMediaDetailsAsync(string referenceId, ReferenceType referenceType, CancellationToken cancellationToken = default);
    Task<Result<MediaDetailsDTO>> GetMovieDetailsAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<Result<MediaDetailsDTO>> GetTvShowDetailsAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MediaDetailsDTO>>> SearchMediaAsync(string query, ReferenceType referenceType, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MediaDetailsDTO>>> SearchMoviesAsync(string query, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MediaDetailsDTO>>> SearchTvShowsAsync(string query, CancellationToken cancellationToken = default);
}