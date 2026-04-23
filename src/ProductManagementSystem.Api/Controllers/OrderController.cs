using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductManagementSystem.Api.Controllers.ServiceFilters;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.RequestParameters;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN,SYSTEM,USER")]
[ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
[EnableRateLimiting("per-user-limit")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMN,SYSTEM")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _orderService.GetAllAsync();

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetAll").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("get-orders")]
    [AllowAnonymous]
    public async Task<IActionResult> GetParameterizedOrders([FromQuery] OrderRequestParameters orderRequestParameters = default)
    {
        try
        {
            var result = await _orderService.GetAllOrdersAsync(orderRequestParameters);

            if (result.IsSuccessStatus)
            {
                Response.Headers.Add("X-Pagination",System.Text.Json.JsonSerializer.Serialize(result.Data.metaData));
            }

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetParameterizedOrders").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("{Id:int}")]
    public async Task<IActionResult> GetById(int Id)
    {
        try
        {
            var result = await _orderService.GetByIdAsync(Id);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetById").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    // [HttpGet("user-orders")]
    // public async Task<IActionResult> GetUserOrders([FromQuery] int UserId, string UserEmailAddress = null)
    // {
    //     try
    //     {
    //         var result = await _orderService.GetUserOrdersAsync(UserId, UserEmailAddress);

    //         return StatusCode((int)result.StatusCode, result);
    //     }
    //     catch (Exception ex)
    //     {
    //         Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetUserOrders").Error(ex, "Error Invoking Endpoint");
    //         return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
    //     }
    // }

    [HttpGet("details/{Id:int}")]
    public async Task<IActionResult> GetOrderDetails(int Id)
    {
        try
        {
            var result = await _orderService.GetOrderDetailsAsync(Id);
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetOrderDetails").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("user-orders")]
    public async Task<IActionResult> GetUserOrders([FromQuery] int UserId)
    {
        try
        {
            var result = await _orderService.GetUserOrdersAsync(UserId);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetUserOrders").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("product/{productId:int}")]
    [Authorize(Roles = "ADMN,SYSTEM")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        try
        {
            var result = await _orderService.GetByProductIdAsync(productId);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetByProduct").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{Id:int}")]
    public async Task<IActionResult> Delete(int Id, bool isSoftDelete = true)
    {
        try
        {
            var result = await _orderService.DeleteAsync(Id, isSoftDelete);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "Delete").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "USER")]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto createOrderDto)
    {
        try
        {
            var result = await _orderService.CreateAsync(createOrderDto);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        { 
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "Create").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateOrderDto updateOrder)
    {
        try
        {
            var result = await _orderService.UpdateAsync(updateOrder);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "Update").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPut("update-status")]
    [Authorize(Roles = "ADMN,SYSTEM")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusDto updateStatus)
    {
        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(updateStatus);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "UpdateStatus").Error(ex, "Error Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
