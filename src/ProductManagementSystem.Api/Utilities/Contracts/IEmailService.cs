using ProductManagementSystem.Shared.DataTransferObjects.MailOperation;

namespace ProductManagementSystem.Api.Utilities.Contracts;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailSenderDto emailToSend);
    Task<bool> RevokeEmailAsync(EmailSenderDto emailToRevoke);
    Task<ProcessedMailResultDto> ProcessQueuedEmails();
}
