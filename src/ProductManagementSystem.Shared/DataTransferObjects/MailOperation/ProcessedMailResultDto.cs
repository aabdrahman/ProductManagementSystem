namespace ProductManagementSystem.Shared.DataTransferObjects.MailOperation;

public record class ProcessedMailResultDto
(int totalProcessed, int totalSuccess, int totalFailure);
