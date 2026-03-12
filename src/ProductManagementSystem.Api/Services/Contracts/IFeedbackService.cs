using ProductManagementSystem.Shared.DataTransferObjects.Feedback;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IFeedbackService
{
    Task<GenericResponse<FeedbackDto>> CreateFeedbackAsync(CreateFeedbackDto createFeedbackDto);
    Task<GenericResponse<IEnumerable<FeedbackDto>>> GetAllFeedbacksAsync();
}
