using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Feedback;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;

namespace ProductManagementSystem.Api.Services;

public class FeedbackService : IFeedbackService
{

    private readonly RepositoryContext _repositoryContext;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public FeedbackService(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
    }
    public async Task<GenericResponse<FeedbackDto>> CreateFeedbackAsync(CreateFeedbackDto createFeedbackDto)
    {
        try
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Information("Create Feedabck - {0}", createFeedbackDto);

            bool isExists = await _repositoryContext.Feedbacks.AnyAsync(f => f.UserEmail == createFeedbackDto.UserEmail && f.Message == createFeedbackDto.Message);

            Feedback feedbackToInsert = new();

            if (createFeedbackDto.UserId.HasValue && createFeedbackDto.UserId.Value != 0)
            {
                User? userPerformingAction = await _repositoryContext.Users.FirstOrDefaultAsync(x => x.Id == createFeedbackDto.UserId.Value);

                feedbackToInsert.Name = userPerformingAction.FirstName + " " + userPerformingAction.LastName;
                feedbackToInsert.UserId = userPerformingAction.Id;
                feedbackToInsert.UserEmail = userPerformingAction.UserEmailAddress;
                feedbackToInsert.Message = createFeedbackDto.Message;
                feedbackToInsert.CreatedAt = DateTime.UtcNow;

            }
            else
            {
                feedbackToInsert.Name = createFeedbackDto.Name;
                feedbackToInsert.Message = createFeedbackDto.Message;
                feedbackToInsert.UserEmail = createFeedbackDto.UserEmail;
                feedbackToInsert.CreatedAt = DateTime.UtcNow;
            }

            await _repositoryContext.Feedbacks.AddAsync(feedbackToInsert);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Information("Feedback created successfully - {0}", feedbackToInsert);

            return GenericResponse<FeedbackDto>.Success(new FeedbackDto
            {
                Name = feedbackToInsert.Name,
                Message = feedbackToInsert.Message,
                EmailAddress = feedbackToInsert.UserEmail,
                CreatedAt = feedbackToInsert.CreatedAt
            }, "Feedback created successfully", System.Net.HttpStatusCode.OK);


        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Error(ex, "An error occurred while creating feedback - {0}", createFeedbackDto);

            return GenericResponse<FeedbackDto>.Failure(null, "An error occurred while creating feedback", System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<GenericResponse<IEnumerable<FeedbackDto>>> GetAllFeedbacksAsync()
    {
        try
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "GetAllFeedbacksAsync").Information("Get All Feedbacks.....");

            List<FeedbackDto> feedbacks = await _repositoryContext.Feedbacks.AsNoTracking().Select(x => new FeedbackDto()
            {
                CreatedAt = x.CreatedAt,
                EmailAddress= x.UserEmail,
                Message = x.Message,
                Name = x.Name

            }).ToListAsync();

            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "GetAllFeedbacksAsync").Information("Feedbacks retrieved successfully - {0}", feedbacks);

            return feedbacks.Any() ?
                GenericResponse<IEnumerable<FeedbackDto>>.Success(feedbacks, "Feedbacks retrieved successfully.", System.Net.HttpStatusCode.OK) :
                GenericResponse<IEnumerable<FeedbackDto>>.Failure(null, "No Feedbacks to retrieve.", System.Net.HttpStatusCode.NotFound);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "GetAllFeedbacksAsync").Error(ex, "An error occurred while retrieving feedbacks");

            return GenericResponse<IEnumerable<FeedbackDto>>.Failure(null, "An error occurred while retrieving feedbacks", System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
