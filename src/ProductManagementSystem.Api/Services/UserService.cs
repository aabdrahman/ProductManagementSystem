using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.User;
using Serilog;
using System.Data.Common;
using System.Net;
using ProductManagementSystem.Api.Entities.StaticValues;

namespace ProductManagementSystem.Api.Services;

public class UserService : IUserService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly IPasswordHasher _passwordHasher;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public UserService(RepositoryContext repositoryContext, IPasswordHasher passwordHasher)
    {
        _repositoryContext = repositoryContext;
        _passwordHasher = passwordHasher;
    }
    public async Task<GenericResponse<UserDto>> CreateAsync(CreateUserDto createUser)
    {
        try
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "CreateAsync").Information("Creating User - {0}", createUser);

            bool isEmailExists = await _repositoryContext.Users.AnyAsync(x => x.UserEmailAddress == createUser.UserEmailAddress.ToUpper());

            if (isEmailExists)
            {
                Log.ForContext(_className, "UserService").ForContext(_methodName, "CreateAsync").Information("User with Email already exists - {0}", createUser.UserEmailAddress);

                return GenericResponse<UserDto>.Failure(null, "User with Email already exists.", HttpStatusCode.Conflict);
            }

            bool isRoleExists = await _repositoryContext.Roles.AnyAsync(x => x.Id == createUser.RoleId);

            if (!isRoleExists)
            {
                Log.ForContext(_className, "UserService").ForContext(_methodName, "CreateAsync").Information("Role to assign to user does not exist - {0}", createUser.RoleId);

                return GenericResponse<UserDto>.Failure(null, $"Role with Id: {createUser.RoleId} does not exist.", HttpStatusCode.BadRequest);
            }

            User userToInsert = new User()
            {
                UserEmailAddress = createUser.UserEmailAddress,
                PhoneNumber = createUser.PhoneNumber,
                Address = createUser.Address,
                FirstName = createUser.FirstName,
                LastName = createUser.LastName,
                MiddleName = createUser.MiddleName,
                RoleId = createUser.RoleId,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = _passwordHasher.HashPassword(createUser.Password)
            };

            await _repositoryContext.Users.AddAsync(userToInsert);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "UserService").ForContext(_methodName, "CreateAsync").Information("User created successfully - {0}", userToInsert);

            UserDto userToReturn = new UserDto()
            {
                Id = userToInsert.Id,
                PhoneNumber = userToInsert.PhoneNumber,
                Email = userToInsert.UserEmailAddress,
                Address = userToInsert.Address,
                AssignedRole = userToInsert?.AssignedRole?.NormalizedName ?? "",
                ActiveStatus = userToInsert.IsActive,
                ConfirmationStatus = userToInsert.IsUserConfirmed,
                FullName = string.Concat(userToInsert.FirstName, " ", userToInsert.LastName)
            };

            return GenericResponse<UserDto>.Success(userToReturn, "User Registration Successful. OTP Sent for account verification", HttpStatusCode.OK);
        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "CreateAsync").Error(ex, "A database error occurred creating user.");
            return GenericResponse<UserDto>.Failure(null, "User Registration Failed.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "CreateAsync").Error(ex, "An error occurred creating user.");
            return GenericResponse<UserDto>.Failure(null, "User registration Failed.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true)
    {
        try
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "DeleteAsync").Information("Delete User with Id - {0}. Soft Deletion - {1}", Id, isSoftDelete);

            User? userToDelete = await _repositoryContext.Users.SingleOrDefaultAsync(x => x.Id == Id);

            if(userToDelete is null)
            {
                Log.ForContext(_className, "UserService").ForContext(_methodName, "DeleteAsync").Information("User with Id does not exist - {0}", Id);

                return GenericResponse<string>.Failure("Operation Failed.", $"User with Id: {Id} does not exist.", HttpStatusCode.NotFound);
            }

            bool isOrderPending = await _repositoryContext.Orders.AnyAsync(x => x.OrderStatus == OrderStatus.Pending || x.OrderStatus == OrderStatus.Processing);

            if (isOrderPending)
            {
                Log.ForContext(_className, "UserService").ForContext(_methodName, "DeleteAsync").Information("User could not be deactivated as one or more orders are still pending or processing - {0}", Id);
                return GenericResponse<string>.Failure("Operation Failed.", "User could not be deactivated as one or more orders are still processing or pending.", HttpStatusCode.Conflict);
            }

            if (isSoftDelete)
            {
                userToDelete.IsActive = false;
            }
            else
            {
                _repositoryContext.Users.Remove(userToDelete);
            }

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "UserService").ForContext(_methodName, "DeleteAsync").Information(isSoftDelete ? "User deactivated successfully - {0}" : "User deleted successfully - {0}", Id);

            return GenericResponse<string>.Success("peration Successful.", isSoftDelete ? "User profile successfully deactivated." : "User profile successfully removed.", HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "DeleteAsync").Error(ex, "An error occurred performing delation operation from database - {0}", Id);
            return GenericResponse<string>.Failure("Operation Faield.", isSoftDelete ? "User profile deactivation failed." : "User profile deletion failed", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "DeleteAsync").Error(ex, "An error occurred performing delation operation. - {0}", Id);
            return GenericResponse<string>.Failure("Operation Faield.", isSoftDelete ? "User profile deactivation failed." : "User profile deletion failed", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<UserDto>>> GetAllAsync()
    {
        try
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetAllAsync").Information("Fetching Users......");

            List<UserDto> users = await _repositoryContext.Users.Select(x => new UserDto()
            {
                Id = x.Id,
                Address = x.Address,
                FullName = string.Concat(x.FirstName, " ", x.LastName),
                PhoneNumber = x.PhoneNumber,
                ActiveStatus = x.IsActive,
                ConfirmationStatus = x.IsUserConfirmed,
                Email = x.UserEmailAddress,
                AssignedRole = x.AssignedRole.NormalizedName
            }).ToListAsync();

            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetAllAsync").Information("User Fetched Successfully - {0}", users);

            return GenericResponse<IEnumerable<UserDto>>.Success(users, "Users Fetched Successfully.", HttpStatusCode.OK);
        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetAllAsync").Error(ex, "An error occurred fetching users from database.");
            return GenericResponse<IEnumerable<UserDto>>.Failure(null, "Users could not be fetched.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetAllAsync").Error(ex, "An error occurred fetching users.");
            return GenericResponse<IEnumerable<UserDto>>.Failure(null, "Users could not be fetched.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<UserDto>> GetUserByIdAsync(int Id)
    {
        try
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetUserByIdAsync").Information("Fetching User with Id - {0}", Id);

            UserDto? user = await _repositoryContext.Users.Select(x => new UserDto()
            {
                Id = x.Id,
                Address = x.Address,
                FullName = string.Concat(x.FirstName, " ", x.LastName),
                PhoneNumber = x.PhoneNumber,
                ActiveStatus = x.IsActive,
                ConfirmationStatus = x.IsUserConfirmed,
                Email = x.UserEmailAddress,
                AssignedRole = x.AssignedRole.NormalizedName
            }).SingleOrDefaultAsync(x => x.Id == Id);

            if(user is null)
            {
                Log.ForContext(_className, "UserService").ForContext(_methodName, "GetUserByIdAsync").Information("User with Id does not exist - {0}", Id);
                return GenericResponse<UserDto>.Failure(null, $"User with Id: {Id} does not exist.", HttpStatusCode.NotFound);
            }

            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetUserByIdAsync").Information("User with Id: {0} fetched successfully - {1}", Id, user);

            return GenericResponse<UserDto>.Success(user, "User details fetched successfully.", HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetUserByIdAsync").Error(ex, "An Error occurred fetching user from database.");
            return GenericResponse<UserDto>.Failure(null, "User details could not be fetched.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "GetUserByIdAsync").Error(ex, "An Error occurred fetching user.");
            return GenericResponse<UserDto>.Failure(null, "User details could not be fetched.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<UserDto>> UpdateAsync(UpdateUserDto updateUser)
    {
        try
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "UpdateAsync").Information("Update User details - {0}", updateUser);

            User? userToUpdate = await _repositoryContext.Users.SingleOrDefaultAsync(x => x.Id == updateUser.Id);

            if(userToUpdate is null)
            {
                Log.ForContext(_className, "UserService").ForContext(_methodName, "UpdateAsync").Information("User with Id does not exist - {0}", updateUser.Id);

                return GenericResponse<UserDto>.Failure(null, $"User with Id: {updateUser.Id} does not exist.", HttpStatusCode.NotFound);
            }

            if(!userToUpdate.UserEmailAddress.Equals(updateUser.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                var isNewEmailExists = await _repositoryContext.Users.AnyAsync(x => x.UserEmailAddress == updateUser.UserEmailAddress.ToUpper());

                if (isNewEmailExists)
                {
                    Log.ForContext(_className, "UserService").ForContext(_methodName, "UpdateAsync").Information("User with email already exists - {0}", updateUser.UserEmailAddress);

                    return GenericResponse<UserDto>.Failure(null, $"New email already assigned to another user.", HttpStatusCode.Conflict);
                }
            }

            if(userToUpdate.RoleId != updateUser.RoleId)
            {
                bool isNewRoleExists = await _repositoryContext.Roles.AnyAsync(x => x.Id == updateUser.RoleId);

                if (!isNewRoleExists)
                {
                    Log.ForContext(_className, "UserService").ForContext(_methodName, "UpdateAsync").Information("Role to assign user does not exist - {0}", updateUser.RoleId);

                    return GenericResponse<UserDto>.Failure(null, $"NRole to assign does not exist - {updateUser.RoleId}", HttpStatusCode.Conflict);
                }

            }

            userToUpdate.UserEmailAddress = updateUser.UserEmailAddress;
            userToUpdate.Address = updateUser.Address;
            userToUpdate.PhoneNumber = updateUser.PhoneNumber;
            userToUpdate.FirstName = updateUser.FirstName;
            userToUpdate.LastName = updateUser.LastName;
            userToUpdate.MiddleName = updateUser.MiddleName;
            userToUpdate.RoleId = updateUser.RoleId;

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "UserService").ForContext(_methodName, "UpdateAsync").Information("User Details Updated Successfully - {0}", userToUpdate);

            UserDto userToReturn = new UserDto()
            {
                Id = userToUpdate.Id,
                PhoneNumber = userToUpdate.PhoneNumber,
                Email = userToUpdate.UserEmailAddress,
                Address = userToUpdate.Address,
                AssignedRole = userToUpdate?.AssignedRole?.NormalizedName ?? "",
                ActiveStatus = userToUpdate.IsActive,
                ConfirmationStatus = userToUpdate.IsUserConfirmed,
                FullName = string.Concat(userToUpdate.FirstName, " ", userToUpdate.LastName)
            };

            return GenericResponse<UserDto>.Success(userToReturn, "User details updated successfully.", HttpStatusCode.OK);

        }
        catch (DbException ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "UpdateAsync").Error(ex, "A database error occurred updating user details.");
            return GenericResponse<UserDto>.Failure(null, "User details update Failed.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserService").ForContext(_methodName, "UpdateAsync").Error(ex, "An error occurred creating user details.");
            return GenericResponse<UserDto>.Failure(null, "User details update Failed.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }
}
