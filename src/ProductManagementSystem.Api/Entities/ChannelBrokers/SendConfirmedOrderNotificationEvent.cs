namespace ProductManagementSystem.Api.Entities.ChannelBrokers;

public record class SendConfirmedOrderNotificationEvent
(int OrderId, string OrderTrackingId);
