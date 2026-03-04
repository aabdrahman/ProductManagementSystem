using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IOrderLineItemService
{
    Task<GenericResponse<string>> AddItemToOrder(int OrderId, CreateOrderLineItemDto createOrderLineItem);
    Task<GenericResponse<string>> RemoveItemFromOrder(int OrderLineItemId, int OrderId);
    Task<GenericResponse<IEnumerable<OrderLineItemDetailsDto>>> GetOrderLineItems(int OrderId);
}
