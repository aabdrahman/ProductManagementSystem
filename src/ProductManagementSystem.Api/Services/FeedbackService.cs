using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Feedback;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Data.Common;

namespace ProductManagementSystem.Api.Services;

public class FeedbackService : IFeedbackService
{

    private readonly RepositoryContext _repositoryContext;
    private readonly IRedisService _redisService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public FeedbackService(RepositoryContext repositoryContext, IRedisService redisService)
    {
        _repositoryContext = repositoryContext;
        _redisService = redisService;
    }
    public async Task<GenericResponse<FeedbackDto>> CreateFeedbackAsync(CreateFeedbackDto createFeedbackDto)
    {
        try
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Information("Create Feedabck - {0}", createFeedbackDto);

            bool isExists = await _repositoryContext.Feedbacks.AnyAsync(f => f.UserEmail == createFeedbackDto.UserEmail && f.Message == createFeedbackDto.Message);

            if (isExists)
            {
                Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Information("Feedback with details already provided by user. Email - {0}, Message - {1}", createFeedbackDto.UserEmail, createFeedbackDto.Message);
                return GenericResponse<FeedbackDto>.Failure(null, "Possible duplicate feedbacks.", System.Net.HttpStatusCode.Conflict);
            }

            Feedback feedbackToInsert = new();

            if (createFeedbackDto.UserId.HasValue && createFeedbackDto.UserId.Value != 0)
            {
                User? userPerformingAction = await _repositoryContext.Users.FirstOrDefaultAsync(x => x.Id == createFeedbackDto.UserId.Value);

                if(userPerformingAction is null)
                {
                    Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Warning("User with Id {0} not found while creating feedback", createFeedbackDto.UserId.Value);
                    return GenericResponse<FeedbackDto>.Failure(null, $"User with Id {createFeedbackDto.UserId.Value} not found while creating feedback", System.Net.HttpStatusCode.NotFound);
                }

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

            var removeFromCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.FeedbackKey);

            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Information("Feedback created successfully - {0}", feedbackToInsert);

            return GenericResponse<FeedbackDto>.Success(new FeedbackDto
            {
                Name = feedbackToInsert.Name,
                Message = feedbackToInsert.Message,
                EmailAddress = feedbackToInsert.UserEmail,
                CreatedAt = feedbackToInsert.CreatedAt.ToLocalTime()
            }, "Feedback created successfully", System.Net.HttpStatusCode.OK);


        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "CreateFeedbackAsync").Error(ex, "A database error occurred while creating feedback - {0}", createFeedbackDto);
            return GenericResponse<FeedbackDto>.Failure(null, "A database error occurred while creating feedback", System.Net.HttpStatusCode.InternalServerError, ex.Message);
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

            var feedbackItemsFromCache = await _redisService.GetItemAsync<List<FeedbackDto>>(RedisCacheHelperClass.FeedbackKey);

            if(feedbackItemsFromCache is not null && feedbackItemsFromCache.Any())
            {
                Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "GetAllFeedbacksAsync").Information("Feedback retrieved from cache - {0}", feedbackItemsFromCache);
                return GenericResponse<IEnumerable<FeedbackDto>>.Success(feedbackItemsFromCache, "Feedbacks retrieved successfully.", System.Net.HttpStatusCode.OK);
            }

            List<FeedbackDto> feedbacks = await _repositoryContext.Feedbacks.AsNoTracking().Select(x => new FeedbackDto()
            {
                CreatedAt = x.CreatedAt.ToLocalTime(),
                EmailAddress= x.UserEmail,
                Message = x.Message,
                Name = x.Name

            }).ToListAsync();

            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "GetAllFeedbacksAsync").Information("Feedbacks retrieved successfully - {0}", feedbacks);

            return feedbacks.Any() ?
                GenericResponse<IEnumerable<FeedbackDto>>.Success(feedbacks, "Feedbacks retrieved successfully.", System.Net.HttpStatusCode.OK) :
                GenericResponse<IEnumerable<FeedbackDto>>.Failure(null, "No Feedbacks to retrieve.", System.Net.HttpStatusCode.NotFound);
        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "GetAllFeedbacksAsync").Error(ex, "A database error occurred while retrieving feedbacks");
            return GenericResponse<IEnumerable<FeedbackDto>>.Failure(null, "A database error occurred while retrieving feedbacks", System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "FeedbackService").ForContext(_methodName, "GetAllFeedbacksAsync").Error(ex, "An error occurred while retrieving feedbacks");

            return GenericResponse<IEnumerable<FeedbackDto>>.Failure(null, "An error occurred while retrieving feedbacks", System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
