using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
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
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _orderService.GetAllAsync();

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetAll").Error(ex, "Error Invoking Ednpoint");
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
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetById").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        try
        {
            var result = await _orderService.GetByProductIdAsync(productId);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "GetByProduct").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{Id:int}")]
    public async Task<IActionResult> Delete(int Id)
    {
        try
        {
            var result = await _orderService.DeleteAsync(Id);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "Delete").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto createOrderDto)
    {
        try
        {
            var result = await _orderService.CreateAsync(createOrderDto);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        { 
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "Create").Error(ex, "Error Invoking Ednpoint");
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
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "Update").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPut("update-status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusDto updateStatus)
    {
        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(updateStatus);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "OrderController").ForContext(_methodName, "UpdateStatus").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
