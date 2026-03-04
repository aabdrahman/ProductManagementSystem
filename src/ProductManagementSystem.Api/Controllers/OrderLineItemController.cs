using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderLineItemController : ControllerBase
{
    private readonly IOrderLineItemService _orderLineItemService;
    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public OrderLineItemController(IOrderLineItemService orderLineItemService)
    {
        _orderLineItemService = orderLineItemService;
    }

    [HttpGet("{OrderId:int}")]
    public async Task<IActionResult> GetOrderLineItems(int OrderId)
    {
        try
        {
            var result = await _orderLineItemService.GetOrderLineItems(OrderId);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderLineItemController").ForContext(_methodName, "GetOrderLineItems").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveOrderLineItem([FromQuery]int OrderId, [FromQuery]int OrderLineItemId)
    {
        try
        {
            var result = await _orderLineItemService.RemoveItemFromOrder(OrderLineItemId, OrderId);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderLineItemController").ForContext(_methodName, "RemoveOrderLineItem").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost("{OrderId:int}")]
    public async Task<IActionResult> AddOrderLineItem(int OrderId, [FromBody] CreateOrderLineItemDto createOrderLineItem)
    {
        try
        {
            var result = await _orderLineItemService.AddItemToOrder(OrderId, createOrderLineItem);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderLineItemController").ForContext(_methodName, "AddOrderLineItem").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
