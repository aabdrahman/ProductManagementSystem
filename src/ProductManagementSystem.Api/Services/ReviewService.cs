using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.Review;
using Serilog;
using System.Data.Common;
using System.Text.Json;

namespace ProductManagementSystem.Api.Services;

public class ReviewService : IReviewService
{
    private readonly RepositoryContext _repositoryContext;
    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public ReviewService(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
    }

    public async Task<GenericResponse<ReviewDto>> AddReviewAsync(CreateReviewDto createReview)
    {

        try
        {
            Log.ForContext(_className, "ReviewService").ForContext(_methodName, "AddReviewAsync").Information($"Create Review - {0}", JsonSerializer.Serialize(createReview));

            Review reviewToInsert = new Review()
            {
                CreatedDate = DateTime.Now,
                ReviewerName = createReview.ReviewerName,
                ReviewText = createReview.Review,
                ProductName = createReview.ProductName,
                Rating = createReview.Rating

            };

            await _repositoryContext.AddAsync(reviewToInsert);

            await _repositoryContext.SaveChangesAsync();

            ReviewDto reviewToReturn = new ReviewDto() { Id = reviewToInsert.Id, ProductName = reviewToInsert.ProductName, Rating = reviewToInsert.Rating, ReviewerName = reviewToInsert .ReviewerName, ReviewText = reviewToInsert.ReviewText };

            return GenericResponse<ReviewDto>.Success(reviewToReturn, "Review Created Successfully.", System.Net.HttpStatusCode.OK);

            throw new NotImplementedException();
        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "ReviewService").ForContext(_methodName, "AddReviewAsync").Error(ex, "An Error Occurred Inserting Review into database.");
            return GenericResponse<ReviewDto>.Failure(null, "An Error Occurred Creating Review.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ReviewService").ForContext(_methodName, "AddReviewAsync").Error(ex, "An Error Occurred Creating Review.");
            return GenericResponse<ReviewDto>.Failure(null, "An Error Occurred Creating Review.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<ReviewDto>>> GetReviewsAsync()
    {

        try
        {
            Log.ForContext(_className, "ReviewService").ForContext(_methodName, "GetReviewsAsync").Information("Getting All Reviews");

            List<ReviewDto> reviews = await _repositoryContext.Reviews
                                    .OrderBy(x => Guid.NewGuid()).ThenByDescending(x => x.Rating)
                                    .Select(x => new ReviewDto()
                                    {
                                        Id = x.Id,
                                        ProductName = x.ProductName,
                                        ReviewerName = x.ReviewerName,
                                        Rating = x.Rating,
                                        ReviewText = x.ReviewText
                                    }).ToListAsync();

            Log.ForContext(_className, "ReviewService").ForContext(_methodName, "GetReviewsAsync").Information($"Fetched Reviews - {0}", JsonSerializer.Serialize(reviews));

            return GenericResponse<IEnumerable<ReviewDto>>.Success(reviews, "Reviews Fetched Successfully", System.Net.HttpStatusCode.OK);
        }
        catch (DbException ex)
        {
            Log.ForContext(_className, "ReviewService").ForContext(_methodName, "GetReviewsAsync").Error(ex, "An Error Occurred Fetching Details from database.");
            return GenericResponse<IEnumerable<ReviewDto>>.Failure(null, "An Error Occurred Fetching Details from database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ReviewService").ForContext(_methodName, "GetReviewsAsync").Error(ex, "An Error Occurred Fetching Details.");
            return GenericResponse<IEnumerable<ReviewDto>>.Failure(null, "An Error Occurred Fetching Details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }

    }
}
