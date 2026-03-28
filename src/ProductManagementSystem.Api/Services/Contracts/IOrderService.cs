using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IOrderService
{
    Task<GenericResponse<OrderDto>> CreateAsync(CreateOrderDto createOrder);
    Task<GenericResponse<OrderDto>> GetByIdAsync(int Id);
    Task<GenericResponse<IEnumerable<OrderDto>>> GetByProductIdAsync(int ProductId);
    Task<GenericResponse<IEnumerable<OrderDto>>> GetAllAsync();
    // Task<GenericResponse<IEnumerable<OrderDto>>> GetUserOrdersAsync(int UserId, string UserEmailAddress = null);
    Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true);
    Task<GenericResponse<string>> CancelOrderAsync(int Id);
    Task<GenericResponse<string>> UpdateOrderStatusAsync(UpdateOrderStatusDto updateOrderStatus);
    Task<GenericResponse<OrderDto>> UpdateAsync(UpdateOrderDto updateOrder);
    Task<GenericResponse<OrderDetailsDto>> GetOrderDetailsAsync(int OrderId);
    Task<GenericResponse<IEnumerable<OrderDto>>> GetUserOrdersAsync(int UserId);
}
