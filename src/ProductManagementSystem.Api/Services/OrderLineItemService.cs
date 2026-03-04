using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Data.Common;
using System.Text.Json;
using ProductManagementSystem.Api.Entities.StaticValues;

namespace ProductManagementSystem.Api.Services;

public class OrderLineItemService : IOrderLineItemService
{
    private readonly RepositoryContext _repositoryContext;
    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public OrderLineItemService(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
    }
    public async Task<GenericResponse<string>> AddItemToOrder(int OrderId, CreateOrderLineItemDto createOrderLineItem)
    {
        try
        {
            Log.ForContext(_methodName, "AddItemToOrder").ForContext(_className, "OrderLineItemService").Information("Adding item to order: {OrderId} with details - {@CreateOrderLineItem}", OrderId, createOrderLineItem);

            bool orderExist = await _repositoryContext.Orders.AnyAsync(x => x.Id == OrderId && (x.OrderStatus == OrderStatus.Pending || x.OrderStatus == OrderStatus.Processing));

            if(!orderExist)
            {
                Log.ForContext(_methodName, "AddItemToOrder").ForContext(_className, "OrderLineItemService").Warning("Order with ID {OrderId} not found.", OrderId);
                return GenericResponse<string>.Failure(null, $"Order with ID {OrderId} not found.", System.Net.HttpStatusCode.NotFound);
            }

            Product? productToOrder = await _repositoryContext.Products.FirstOrDefaultAsync(x => x.Id == createOrderLineItem.ProductId);

            if(productToOrder is null)
            {
                Log.ForContext(_methodName, "AddItemToOrder").ForContext(_className, "OrderLineItemService").Warning("Order Product does not exist. Id - {0}", createOrderLineItem.ProductId);
                return GenericResponse<string>.Failure("Operation Failed.", $"Product with Id: {createOrderLineItem.ProductId} does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            if(productToOrder.CurrentCount < createOrderLineItem.QuantityOrdered)
            {
                Log.ForContext(_methodName, "AddItemToOrder").ForContext(_className, "OrderLineItemService").Warning("Product could not be ordered as the current stock: {0} is not enough to cater for order - {1}", productToOrder.CurrentCount, createOrderLineItem.QuantityOrdered);
                return GenericResponse<string>.Failure("Operation Failed.", "Product does not have eniugh stock to cater for the ordered quantity.", System.Net.HttpStatusCode.BadRequest);
            }

            productToOrder.CurrentCount -= createOrderLineItem.QuantityOrdered;

            var orderLineItem = new OrderLineItem
            {
                ProductId = createOrderLineItem.ProductId,
                QuantityOrdered = createOrderLineItem.QuantityOrdered,
                OrderId = OrderId
            };


            await _repositoryContext.OrderLineItems.AddAsync(orderLineItem);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_methodName, "AddItemToOrder").ForContext(_className, "OrderLineItemService").Information("Successfully added item to order with ID {OrderId}.", OrderId);

            return GenericResponse<string>.Success("Operation Successful.", $"Successfully added item to order with ID {OrderId}.", System.Net.HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_methodName, "AddItemToOrder").ForContext(_className, "OrderLineItemService").Error(ex, "Database error occurred while adding item to order with ID {OrderId}.", OrderId);
            return GenericResponse<string>.Failure("Operation Failed", $"Database error occurred while adding item to order with ID {OrderId}.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "AddItemToOrder").ForContext(_className, "OrderLineItemService").Error(ex, "An error occurred while adding item to order with ID {OrderId}.", OrderId);
            return GenericResponse<string>.Failure("Operation Failed", $"An error occurred while adding item to order with ID {OrderId}.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<OrderLineItemDetailsDto>>> GetOrderLineItems(int OrderId)
    {
        try
        {
            Log.ForContext(_methodName, "GetOrderLineItems").ForContext(_className, "OrderLineItemService").Information("Getting Order Line Items for Order - {0}", OrderId);

            var orderItems = await _repositoryContext.OrderLineItems.Where(x => x.OrderId == OrderId)
                                                        .Select(x => new OrderLineItemDetailsDto
                                                        {
                                                            Id = x.Id,
                                                            ProductName = x.OrderedProduct.NormalizedName,
                                                            OrderCount = x.QuantityOrdered,
                                                            IsActive = x.IsActive
                                                        }).ToListAsync();

            Log.ForContext(_methodName, "GetOrderLineItems").ForContext(_className, "OrderLineItemService").Information("Order Line Items for Order: {0} - {1}", OrderId, JsonSerializer.Serialize(orderItems));

            return GenericResponse<IEnumerable<OrderLineItemDetailsDto>>.Success(orderItems, $"Successfully retrieved order line items for order with ID {OrderId}.", System.Net.HttpStatusCode.OK);
        }
        catch(DbException ex)
        {
            Log.ForContext(_methodName, "GetOrderLineItems").ForContext(_className, "OrderLineItemService").Error(ex, "Database error occurred while retrieving order line items for order with ID {OrderId}.", OrderId);
            return GenericResponse<IEnumerable<OrderLineItemDetailsDto>>.Failure(null, $"Database error occurred while retrieving order line items for order with ID {OrderId}.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "GetOrderLineItems").ForContext(_className, "OrderLineItemService").Error(ex, "An error occurred while retrieving order line items for order with ID {OrderId}.", OrderId);
            return GenericResponse<IEnumerable<OrderLineItemDetailsDto>>.Failure(null, $"An error occurred while retrieving order line items for order with ID {OrderId}.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> RemoveItemFromOrder(int OrderLineItemId, int OrderId)
    {
        try
        {
            Log.ForContext(_methodName, "RemoveItemFromOrder").ForContext(_className, "OrderLineItemService").Information("Remove Order Item. Order Id: {0}, Order Item Id: {1}", OrderId, OrderLineItemId);

            OrderLineItem? orderLineItemToRemove = await _repositoryContext.OrderLineItems.Include(x => x.OrderedProduct).FirstOrDefaultAsync(x => x.Id == OrderLineItemId && x.OrderId == OrderId);

            if(orderLineItemToRemove is null)
            {
                Log.ForContext(_methodName, "RemoveItemFromOrder").ForContext(_className, "OrderLineItemService").Information("Order Item with ID {0} not found for Order with ID {1}.", OrderLineItemId, OrderId);
                return GenericResponse<string>.Failure(null, $"Order Item with ID {OrderLineItemId} not found for Order with ID {OrderId}.", System.Net.HttpStatusCode.NotFound);
            }


            orderLineItemToRemove.OrderedProduct.CurrentCount += orderLineItemToRemove.QuantityOrdered;

            _repositoryContext.OrderLineItems.Remove(orderLineItemToRemove);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_methodName, "RemoveItemFromOrder").ForContext(_className, "OrderLineItemService").Information("Successfully removed Order Item with ID {0} from Order with ID {1}.", OrderLineItemId, OrderId);

            return GenericResponse<string>.Success("Operation Successful.", $"Successfully removed Order Item with ID {OrderLineItemId} from Order with ID {OrderId}.", System.Net.HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_methodName, "RemoveItemFromOrder").ForContext(_className, "OrderLineItemService").Error(ex, "Database error occurred while removing order item with ID {0} from order with ID {1}.", OrderLineItemId, OrderId);
            return GenericResponse<string>.Failure("Operation Failed", $"Database error occurred while removing order item with ID {OrderLineItemId} from order with ID {OrderId}.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "RemoveItemFromOrder").ForContext(_className, "OrderLineItemService").Error(ex, "An error occurred while removing order item with ID {0} from order with ID {1}.", OrderLineItemId, OrderId);
            return GenericResponse<string>.Failure("Operation Failed", $"An error occurred while removing order item with ID {OrderLineItemId} from order with ID {OrderId}.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }
}
