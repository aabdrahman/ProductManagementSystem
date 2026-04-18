using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using ProductManagementSystem.Api.Entities.Models;
using System.Security.Claims;

namespace ProductManagementSystem.Api.Controllers.AuthRequirements;

public class GetUserDetailsRequirement : AuthorizationHandler<OperationAuthorizationRequirement, User>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, User resource)
    {
        if((context.User.IsInRole("ADMIN") || context.User.IsInRole("SYSTEM")) && requirement.Name == Operations.Read.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if(context.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value == resource.Id.ToString() && requirement.Name == Operations.Read.Name)
        {
            
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}


public class UpdateUserDetailsRequirement : AuthorizationHandler<OperationAuthorizationRequirement, User>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, User resource)
    {
        if((context.User.IsInRole("ADMIN") || context.User.IsInRole("SYSTEM")) && requirement.Name == Operations.Update.Name)
        {

            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if(context.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value == resource.Id.ToString() && requirement.Name == Operations.Update.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}

public class DeleteUserDetailsRequirement : AuthorizationHandler<OperationAuthorizationRequirement, User>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, User resource)
    {

        if((context.User.IsInRole("SYSTEM") || context.User.IsInRole("SYSTEM")) &&  requirement.Name == Operations.Delete.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if(context.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value == resource.Id.ToString() && requirement.Name == Operations.Delete.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}