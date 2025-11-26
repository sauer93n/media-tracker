using Application.DTO;
using FluentResults;
using Gridify;

namespace Application.Interface;

public interface IMediaService
{
    Task<Result<MediaDetailsDTO>> GetMediaDetailsAsync(string referenceId, ReferenceType referenceType, CancellationToken cancellationToken = default);
    Task<Result<MediaDetailsDTO>> GetMovieDetailsAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<Result<MediaDetailsDTO>> GetTvShowDetailsAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MediaDetailsDTO>>> SearchMediaAsync(string query, GridifyQuery gridifyQuery, ReferenceType referenceType, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MediaDetailsDTO>>> SearchMoviesAsync(string query, GridifyQuery gridifyQuery, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MediaDetailsDTO>>> SearchTvShowsAsync(string query, GridifyQuery gridifyQuery, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> GetPosterImageAsync(string referenceId, ReferenceType referenceType, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> GetMoviePosterImageAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> GetTvShowPosterImageAsync(string referenceId, CancellationToken cancellationToken = default);
}