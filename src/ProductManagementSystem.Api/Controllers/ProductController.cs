using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Product;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private string _methodName = "MethodName";
    private string _className = "ClassName";
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _productService.GetAllAsync();
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "GetAll").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("{Id:int}")]
    public async Task<IActionResult> GetById(int Id)
    {
        try
        {
            var result = await _productService.GetByIdAsync(Id);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "GetById").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("category/{categoryId:int}")]
    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        try
        {
            var result = await _productService.GetByCategoryIdAsync(categoryId);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "GetByCategory").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{Id:int}")]
    public async Task<IActionResult> Delete(int Id, bool isSoftDelete = true)
    {
        try
        {
            var result = await _productService.DeleteAsync(Id, isSoftDelete);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "Delete").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto productToCreate)
    {
        try
        {
            var result = await _productService.CreateAsync(productToCreate);
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "Create").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProductDto productToUpdate)
    {
        try
        {
            var result = await _productService.UpdateAsync(productToUpdate);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "Update").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPatch("update-stock")]
    public async Task<IActionResult> UpdateStock([FromBody] UpdateProductStockDto productToUpdateStock)
    {
        try
        {
            var result = await _productService.UpdateStockAsync(productToUpdateStock);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "UpdateStock").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
