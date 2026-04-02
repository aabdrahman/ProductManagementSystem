namespace ProductManagementSystem.Shared.DataTransferObjects.MailOperation;

public record class EmailSenderDto
(string Subject, string Content, List<string> Recipients, bool isHtml = false, EmailPriority priority = EmailPriority.Low);

public enum EmailPriority
{
    Low,
    Medium,
    High
}
