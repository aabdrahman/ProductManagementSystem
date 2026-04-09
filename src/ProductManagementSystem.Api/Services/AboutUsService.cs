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

public class AboutUsService : IAboutUsService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly IRedisService _redisService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private string _className = "ClassName";
    private string _methodName = "MethodName";

    public AboutUsService(RepositoryContext repositoryContext, IRedisService redisService, IHttpContextAccessor httpContextAccessor)
    {
        _repositoryContext = repositoryContext;
        _redisService = redisService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<GenericResponse<string>> CreateAsync(CreateAboutUsDto createAboutUs, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(CreateAsync)).Information("Create New About Us - {0}", createAboutUs);

            bool isExists = await _repositoryContext.AboutUsDetails.AnyAsync(x => x.Title == createAboutUs.Title && x.Content ==createAboutUs.ContentDetails);

            if (isExists)
            {
                Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(CreateAsync)).Information("An item exists with the same title and content details");
                return GenericResponse<string>.Failure("Operation Failed.", "Content with similar details already exists.", System.Net.HttpStatusCode.Conflict);
            }

            AboutUsDetail entityToInsert = new AboutUsDetail()
            {
                Content = createAboutUs.ContentDetails,
                Title = createAboutUs.Title,
                CreatedBy = _httpContextAccessor.HttpContext?.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value.ToString() ?? "0"
            };

            await _repositoryContext.AboutUsDetails.AddAsync(entityToInsert);

            await _repositoryContext.SaveChangesAsync();

            var removeCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.AboutUsContentKey);

            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(CreateAsync)).Information("About Us Content details created successfully - {0}. Remove Cache Content - {1}", entityToInsert, removeCache);

            return GenericResponse<string>.Success("Operation Successful", "Item successfully created.", System.Net.HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(CreateAsync)).Error(ex, "An Error Occurred while creating about us content");
            return GenericResponse<string>.Failure("Operation Failed.", "An error occurred while performing operation.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message  });

        }
    }

    public async Task<GenericResponse<string>> DeleteAsync(Guid Id, bool isSoftDelete = false, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(DeleteAsync)).Information("Removing About us content - {0}. Is Soft Delete : {1}", Id, isSoftDelete);

            var itemToRemove = await _repositoryContext.AboutUsDetails.FirstOrDefaultAsync(x => x.Id == Id);

            if(itemToRemove is null)
            {
                Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(DeleteAsync)).Information("Content Item with specified Id does not exist - {0}", Id);
                return GenericResponse<string>.Failure("Operation Failed", "Content with specidied Id does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            if (isSoftDelete)
            {
                itemToRemove.IsDeleted = true;
                itemToRemove.DeletedAt = DateTime.UtcNow;
                itemToRemove.DeletedBy = _httpContextAccessor.HttpContext?.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value.ToString() ?? "0";
            }
            else
            {
                _repositoryContext.AboutUsDetails.Remove(itemToRemove);
            }

            await _repositoryContext.SaveChangesAsync();

            var removeCached = await _redisService.RemoveItemAsync(RedisCacheHelperClass.AboutUsContentKey);

            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(DeleteAsync)).Information("About us content deletion operation successful. Id - {0}, Operation Type: {1}. Remove cached - {2}", Id, isSoftDelete, removeCached);

            return GenericResponse<string>.Success("Operation Successful.", "Content deletion operation successful.", System.Net.HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(DeleteAsync)).Error(ex, "An error occurred performing delation operation.");
            return GenericResponse<string>.Failure("Operation Failed.", "An error occurred eprforming deletion operation.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<AboutUsDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(GetAllAsync)).Information("Fetch all about us content details");

            var aboutUsContentCache = await _redisService.GetItemAsync<IEnumerable<AboutUsDto>>(RedisCacheHelperClass.AboutUsContentKey);

            if(aboutUsContentCache is not null && aboutUsContentCache.Any())
            {
                Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(GetAllAsync)).Information("About us item fetched form cache - {0}", aboutUsContentCache);
                return GenericResponse<IEnumerable<AboutUsDto>>.Success(aboutUsContentCache, "Content fetched successfully.", System.Net.HttpStatusCode.OK);
            }


            List<AboutUsDto> contentItems = await _repositoryContext.AboutUsDetails.AsNoTracking().Select(x => new AboutUsDto(x.Id, x.Title, x.Content)).ToListAsync(cancellationToken);

            var setItemToCache = await _redisService.SetItemAsync<IEnumerable<AboutUsDto>>(contentItems, RedisCacheHelperClass.AboutUsContentKey, 18400);

            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(GetAllAsync)).Information("Fetched contents - {0}. Set Item returns - {1}", contentItems, setItemToCache);

            return GenericResponse<IEnumerable<AboutUsDto>>.Success(contentItems, "Content fetched successfully.", System.Net.HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(GetAllAsync)).Error(ex, "An error occurred fetching about us content.");
            return GenericResponse<IEnumerable<AboutUsDto>>.Failure(null, "An error occurred while performing operation.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> UpdateAsync(UpdateAboutUsDto updateAboutUs, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(UpdateAsync)).Information("Update about us content - {0}", updateAboutUs);

            var aboutUsContentToUpdate = await _repositoryContext.AboutUsDetails.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == updateAboutUs.Id);

            if(aboutUsContentToUpdate is null)
            {
                Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(UpdateAsync)).Information("About us content with id does not exist - {0}", updateAboutUs);
                return GenericResponse<string>.Failure("Operation Failed.", "About us item content with id does not exist", System.Net.HttpStatusCode.NotFound);
            }

            aboutUsContentToUpdate.Title = updateAboutUs.Title;
            aboutUsContentToUpdate.Content = updateAboutUs.ContentDetails;
            aboutUsContentToUpdate.IsDeleted = false;

            await _repositoryContext.SaveChangesAsync();

            var removeCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.AboutUsContentKey);

            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(UpdateAsync)).Information("Update about us item content successful - {0}. Remove Cache - {1}", aboutUsContentToUpdate.Id, removeCache);

            return GenericResponse<string>.Success("Operation Successful", "About us item content updated successfully.", System.Net.HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsService)).ForContext(_methodName, nameof(UpdateAsync)).Error(ex, "An error occurred updating about us content");
            return GenericResponse<string>.Failure("Operation Failed.", "An error occurred updating about us item content.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }
}
