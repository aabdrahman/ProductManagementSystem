using ProductManagementSystem.Api.Controllers;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Utilities.Contracts;

namespace ProductManagementSystem.Api.Utilities;

public class EmailVerificationLinkFactory : IEmailVerificationLinkFactory
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public EmailVerificationLinkFactory(IHttpContextAccessor contextAccessor, LinkGenerator linkGenerator)
    {
        _contextAccessor = contextAccessor;
        _linkGenerator = linkGenerator;
    }
    public string GetEmailVerificationLink(UserOrderVerificationToken userOrderVerificationToken)
    {
        string emailVerificationLink = _linkGenerator.GetUriByAction(_contextAccessor.HttpContext, action: "VerifyToken", controller: nameof(UserOrderVerificationController).Replace("Controller", string.Empty), new { verification_token = $"{userOrderVerificationToken.Id.ToString()}={userOrderVerificationToken.VerificationToken}"}) ??
                                                                                throw new ArgumentNullException(nameof(emailVerificationLink), "Email verification link could not be geenrated");

        return emailVerificationLink;
    }
}
