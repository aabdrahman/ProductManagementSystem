using ProductManagementSystem.Shared.DataTransferObjects.MailOperation;

namespace ProductManagementSystem.Api.Entities.ChannelBrokers;

public record class SendPriorityMailEvent
(EmailSenderDto EmailDetails);
