using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Security.Claims;

namespace ProductManagementSystem.Api.Services;

public class WhyChooseUsService : IWhyChooseUsService
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly RepositoryContext _repositoryContext;
    private readonly IRedisService _redisService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public WhyChooseUsService(IHttpContextAccessor contextAccessor, RepositoryContext repositoryContext, IRedisService redisService)
    {
        _contextAccessor = contextAccessor;
        _repositoryContext = repositoryContext;
        _redisService = redisService;
    }

    public async Task<GenericResponse<string>> CreateAsync(CreateWhyChooseUsDto createWhyChooseUs, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(CreateAsync)).Information("Create Why choose us content - 0}", createWhyChooseUs);

            var isExists = await _repositoryContext.WhyChooseUsDetails.AnyAsync(x => x.Title == createWhyChooseUs.Title && x.Content == x.Content);

            if (isExists)
            {
                Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(CreateAsync)).Information("Content with specified details alrady exist.");
                return GenericResponse<string>.Failure("Operation Failed.", "Content with details already exists.", System.Net.HttpStatusCode.Conflict);
            }

            WhyChooseUsDetail itemToInsert = new WhyChooseUsDetail()
            {
                Title = createWhyChooseUs.Title,
                Content = createWhyChooseUs.ContentDetails,
                CreatedBy = _contextAccessor.HttpContext?.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0"
            };

            await _repositoryContext.WhyChooseUsDetails.AddAsync(itemToInsert);

            await _repositoryContext.SaveChangesAsync();

            var removeCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.WhyChooseUsContentKey);

            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(CreateAsync)).Information("Why choose us content created successfully - {0}. Remove cache - {1}", itemToInsert.Id, removeCache);

            return GenericResponse<string>.Success("Operation Successful", "Content created successfully.", System.Net.HttpStatusCode.OK);

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(CreateAsync)).Error(ex, "An error occurred creating why choose us content.");
            return GenericResponse<string>.Failure("Operation Failed.", "An error occurred creating content.", System.Net.HttpStatusCode.InternalServerError, new { Messaeg = ex.Message });
            throw;
        }
    }

    public async Task<GenericResponse<string>> DeleteAsync(Guid Id, bool isSoftDelete = false, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(DeleteAsync)).Information("Perform Deletion Operation - {Id}. Is Soft Delete: {1}", Id, isSoftDelete);

            var itemToPerformOperation = await _repositoryContext.WhyChooseUsDetails.FirstOrDefaultAsync(x => x.Id == Id);

            if(itemToPerformOperation is null)
            {
                Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(DeleteAsync)).Information("Why choose us content item does not exist - {0}", Id);
                return GenericResponse<string>.Failure("Operation Failed.", "Content item does not exist.", System.Net.HttpStatusCode.OK);
            }

            if (isSoftDelete)
            {
                itemToPerformOperation.IsDeleted = true;
                itemToPerformOperation.DeletedBy = _contextAccessor.HttpContext.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0";
                itemToPerformOperation.DeletedAt = DateTime.UtcNow;

            }
            else
            {
                _repositoryContext.WhyChooseUsDetails.Remove(itemToPerformOperation);
            }

            await _repositoryContext.SaveChangesAsync();

            var removeCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.WhyChooseUsContentKey);

            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(DeleteAsync)).Information("Deletion operation for - {0}, Is Soft Delete: {1}. Remove Cache: {2}", Id, isSoftDelete, removeCache);

            return GenericResponse<string>.Success("Operation Successful.", "Deletion Operation Completed successfully.", System.Net.HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(DeleteAsync)).Error(ex, "An error occurred performing deletion operation.");
            return GenericResponse<string>.Failure("Operation Failed.", "An error occurred performing operation.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<WhyChooseUsDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(GetAllAsync)).Information("Get All Why choose us content");

            var itemsFromCache = await _redisService.GetItemAsync<IEnumerable<WhyChooseUsDto>>(RedisCacheHelperClass.WhyChooseUsContentKey);

            if(itemsFromCache is not null && itemsFromCache.Any())
            {
                Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(GetAllAsync)).Information("Item fetched fom cache - {0}", itemsFromCache);
                return GenericResponse<IEnumerable<WhyChooseUsDto>>.Success(itemsFromCache, "Content Fetched successfully.", System.Net.HttpStatusCode.OK);
            }

            var contentItems = await _repositoryContext.WhyChooseUsDetails.Select(x => new WhyChooseUsDto(x.Id, x.Title, x.Content)).ToListAsync();

            if (contentItems.Any())
            {
                var setItemsToCache = await _redisService.SetItemAsync<IEnumerable<WhyChooseUsDto>>(contentItems, RedisCacheHelperClass.WhyChooseUsContentKey, 18400);
                Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(GetAllAsync)).Information("Item content fetched successsfully - {0}. Set Item to cache - {1}", contentItems, setItemsToCache);

                return GenericResponse<IEnumerable<WhyChooseUsDto>>.Success(contentItems, "Content fetched successfully.", System.Net.HttpStatusCode.OK);
            }
            else
            {
                Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(GetAllAsync)).Information("Item content fetched successsfully - {0}", contentItems);

                return GenericResponse<IEnumerable<WhyChooseUsDto>>.Success(contentItems, "No content to display.", System.Net.HttpStatusCode.NotFound);
            }

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(GetAllAsync)).Error(ex, "An errror occurred fetching content details.");
            return GenericResponse<IEnumerable<WhyChooseUsDto>>.Failure(null, "An error occurred fetching content details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> UpdateAsync(UpdateWhyChooseUsDto updateWhyChooseUs, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(UpdateAsync)).Information("Update Why choose us item content - {0}", updateWhyChooseUs);

            var itemToUpdate = await _repositoryContext.WhyChooseUsDetails.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == updateWhyChooseUs.Id);

            if(itemToUpdate is null)
            {
                Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(UpdateAsync)).Information("");
                return GenericResponse<string>.Failure("Operation Failed", "Item content with Id does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            itemToUpdate.IsDeleted = false;
            itemToUpdate.Title = updateWhyChooseUs.Title;
            itemToUpdate.Content = updateWhyChooseUs.ContentDetails;

            await _repositoryContext.SaveChangesAsync();

            var removeCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.WhyChooseUsContentKey);

            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(UpdateAsync)).Information("Why choose us item content updated successfully - {0}. Remove cache: {1}", itemToUpdate.Id, removeCache);

            return GenericResponse<string>.Success("Operation Successful.", "Item content updated successfully.", System.Net.HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsService)).ForContext(_methodName, nameof(UpdateAsync)).Error(ex, "An error occurred updating why choose us item content.");
            return GenericResponse<string>.Failure("Operation Failed.", "An error occurred updating item content.", System.Net.HttpStatusCode.InternalServerError, new { message = ex.Message });
            throw;
        }
    }
}
