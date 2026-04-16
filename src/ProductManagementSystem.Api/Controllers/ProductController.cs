using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Controllers.ServiceFilters;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Product;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN,SYSTEM,USER")]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
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

    [HttpGet("get-product-update-details/{Id:int}")]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> GetProductUpdateDetails(int Id)
    {
        try
        {
            var result = await _productService.GetProductUpdateDetails(Id);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "GetProductUpdateDetails").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("collection")]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> GetProductCollection([FromQuery] IEnumerable<int> Ids)
    {
        try
        {
            var result = await _productService.GetMultipleProductsAsync(Ids.ToList());

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductController").ForContext(_methodName, "GetProductCollection").Error(ex, "An Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{Id:int}")]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
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
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
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
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
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
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
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
