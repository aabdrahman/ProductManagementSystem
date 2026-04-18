using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using ProductManagementSystem.Api.Entities.Models;
using System.Security.Claims;

namespace ProductManagementSystem.Api.Controllers.AuthRequirements;

public class DeleteOrderDetailsRequirementHandler : AuthorizationHandler<OperationAuthorizationRequirement, Order>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Order resource)
    {
        if((context.User.IsInRole("ADMIN") || context.User.IsInRole("SYSTEM")) && requirement.Name == Operations.Delete.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if(context.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value == resource.UserId.ToString() && requirement.Name == Operations.Delete.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}

public class UpdateOrderDetailsRequirementHandler : AuthorizationHandler<OperationAuthorizationRequirement, Order>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Order resource)
    {
        if ((context.User.IsInRole("ADMIN") || context.User.IsInRole("SYSTEM")) && requirement.Name == Operations.Update.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value == resource.UserId.ToString() && requirement.Name == Operations.Update.Name)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}


public static class Operations
{
    public static OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement() { Name = "Delete" };
    public static OperationAuthorizationRequirement Update = new OperationAuthorizationRequirement() { Name = "Update" };
    public static OperationAuthorizationRequirement Read = new OperationAuthorizationRequirement() { Name = "Read" };
}
