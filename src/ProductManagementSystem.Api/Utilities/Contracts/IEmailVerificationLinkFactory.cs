using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Utilities.Contracts;

public interface IEmailVerificationLinkFactory
{
    string GetEmailVerificationLink(UserOrderVerificationToken userOrderVerificationToken);
}
