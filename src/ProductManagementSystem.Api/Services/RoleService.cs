using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.Role;
using Serilog;
using System.Data.Common;
using System.Net;
using System.Security.Claims;

namespace ProductManagementSystem.Api.Services;

public class RoleService : IRoleService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRedisService _redisService;
    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public RoleService(RepositoryContext repositoryContext, IHttpContextAccessor httpContextAccessor, IRedisService redisService)
    {
        _repositoryContext = repositoryContext;
        _httpContextAccessor = httpContextAccessor;
        _redisService = redisService;
    }
    public async Task<GenericResponse<RoleDto>> CreateRoleAsync(string roleName)
    {
        try
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Information("Creating Role - {0}", roleName);

            string loggedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";

            if(!int.TryParse(loggedInUserId, out int userId) || loggedInUserId == "0")
            {
                Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Information("User details could not be fetched. Invalid User Id - {0}", loggedInUserId);
                return GenericResponse<RoleDto>.Failure(null, "Role Creation Failed.", HttpStatusCode.Unauthorized);
            }

            bool isUserExists = await _repositoryContext.Users.AnyAsync(x => x.Id == userId);

            if (!isUserExists)
            {
                Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Information("User with Id - {0} does not exist", userId);
                return GenericResponse<RoleDto>.Failure(null, $"User with Id does not exist. Invalid logged in user.", HttpStatusCode.Conflict);
            }

            bool isExistsName = await _repositoryContext.Roles.AnyAsync(x => x.NormalizedName.Equals(roleName.ToUpper()));

            if (isExistsName)
            {
                Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Information("Role Exists for provided name - {0}", roleName);
                return GenericResponse<RoleDto>.Failure(null, $"Role exists for provided name: {roleName}", HttpStatusCode.Conflict);
            }

            Role roleToInsert = new Role()
            {
                Name = roleName,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            await _repositoryContext.Roles.AddAsync(roleToInsert);

            await _repositoryContext.SaveChangesAsync();

            var removeItemFromCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.RolesKey);

            RoleDto roleToReturn = new RoleDto() { RoleId = roleToInsert.Id, RoleName = roleToInsert.NormalizedName };

            Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Information("Role Created Successfull - {0}", roleToReturn);

            return GenericResponse<RoleDto>.Success(roleToReturn, "Role Created Successfully.", HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Error(ex, "An error occurred inserting record to database.");
            return GenericResponse<RoleDto>.Failure(null, "An Error Occurred inserting record to database", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Error(ex, "An error occurred creating role");
            return GenericResponse<RoleDto>.Failure(null, "An error occurred creating role", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true)
    {
        try
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "DeleteAsync").Information("Delete Role - {0}, Soft Deletion - {1}", Id, isSoftDelete);

            Role? roleToDelete = await _repositoryContext.Roles.SingleOrDefaultAsync(x => x.Id == Id);

            if(roleToDelete is null)
            {
                Log.ForContext(_className, "RoleService").ForContext(_methodName, "DeleteAsync").Information("Role with Id does not exist - {0}", Id);
                return GenericResponse<string>.Failure("Operation Fialed.", $"Roel with Id does not exist - {Id}", HttpStatusCode.NotFound);
            }

            bool isUserExists = await _repositoryContext.Users.AnyAsync(x => x.RoleId == Id);

            if (isUserExists)
            {
                Log.ForContext(_className, "RoleService").ForContext(_methodName, "DeleteAsync").Information("Role could not be deleted as one or more users are still linked");
                return GenericResponse<string>.Failure("Operation Failed.", "One or more users are assigned to role.", HttpStatusCode.Conflict);
            }

            if(isSoftDelete)
            {
                roleToDelete.IsActive = false;
            }
            else
            {
                _repositoryContext.Roles.Remove(roleToDelete);
            }

            await _repositoryContext.SaveChangesAsync();

            var removeRoleFromCache = await _redisService.RemoveMultiple(RedisCacheHelperClass.GetRoleCacheKey(Id), RedisCacheHelperClass.RolesKey);

            Log.ForContext(_className, "RoleService").ForContext(_methodName, "DeleteAsync").Information(isSoftDelete ? "Role deactivated successfully - {0} Remove From Cache - {1}" : "Role deleted successfully - {0} Remove From Cache - {1}", Id, removeRoleFromCache);

            return GenericResponse<string>.Success("Operation Failed.", isSoftDelete ? "Role deactivated successfully." : "Role deleted successfully.", HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Error(ex, isSoftDelete ? "An Error occurred deactivating role in database - {0}." : "An error occurred deleting role from database - {0}.", Id);
            return GenericResponse<string>.Failure(null, isSoftDelete ? $"An error occurred deactivating record in database." : $"An Error Occurred deleting record from database.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "CreateRoleAsync").Error(ex, isSoftDelete ? "An Error occurred deactivating role - {0}." : "An error occurred deleting role - {0}.", Id);
            return GenericResponse<string>.Failure(null, isSoftDelete ? $"An error occurred deactivating role." : $"An Error Occurred deleting role.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<RoleDto>>> GetAllAsync()
    {
        try
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "GetAllAsync").Information("Fetching All Roles.....");

            var rolesFromCache = await _redisService.GetItemAsync<List<RoleDto>>(RedisCacheHelperClass.RolesKey);

            if(rolesFromCache is not null && rolesFromCache.Any())
            {
                Log.ForContext(_className, "RoleService").ForContext(_methodName, "GetAllAsync").Information("Roles Fetched from cache - {0}", rolesFromCache);
                return GenericResponse<IEnumerable<RoleDto>>.Success(rolesFromCache, "Roles Fetched Successfully.", HttpStatusCode.OK);
            }

            List<RoleDto> roles = await _repositoryContext.Roles.AsNoTracking().Select(x => new RoleDto()
            {
                RoleId = x.Id,
                RoleName = x.NormalizedName
            }).ToListAsync();

            var setRolesToCache = await _redisService.SetItemAsync<List<RoleDto>>(roles, RedisCacheHelperClass.RolesKey, 18400);

            Log.ForContext(_className, "RoleService").ForContext(_methodName, "GetAllAsync").Information("Roles successfully fetched - {0}. Set Roles To Cache Returns - {1}", roles, setRolesToCache);

            return GenericResponse<IEnumerable<RoleDto>>.Success(roles, "Roles Fetched Successfully.", HttpStatusCode.OK);
        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "GetAllAsync").Error(ex, "An Error Fetching roles from database.");
            return GenericResponse<IEnumerable<RoleDto>>.Failure(null, "An Error Occurred Fetching roles from database.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "RoleService").ForContext(_methodName, "GetAllAsync").Error(ex, "An Error Fetching roles.");
            return GenericResponse<IEnumerable<RoleDto>>.Failure(null, "An Error Fetching roles.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }
}