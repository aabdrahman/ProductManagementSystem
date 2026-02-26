using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.Review;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IReviewService
{
    Task<GenericResponse<IEnumerable<ReviewDto>>> GetReviewsAsync();
    Task<GenericResponse<ReviewDto>> AddReviewAsync(CreateReviewDto createReview);
}
