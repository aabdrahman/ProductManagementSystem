using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductManagementSystem.Api.Controllers.ServiceFilters;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.ProductCategory;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN,SYSTEM")]
[EnableRateLimiting("per-user-limit")]
public class ProductCategoryController : ControllerBase
{
    private string _methodName = "MethodName";
    private string _className = "ClassName";
    private IProductCategoryService _productCategoryService;
    public ProductCategoryController(IProductCategoryService productCategoryService)
    {
        _productCategoryService = productCategoryService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _productCategoryService.GetAllAsync();

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryController").ForContext(_methodName, "GetAll").Error(ex, "Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("{Id:int}")]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> GetById(int Id)
    {
        try
        {
            var result = await _productCategoryService.GetByIdAsync(Id);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryController").ForContext(_methodName, "GetById").Error(ex, "Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{Id:int}")]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> DeleteById(int Id)
    {
        try
        {
            var result = await _productCategoryService.DeleteAsync(Id);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryController").ForContext(_methodName, "DeleteById").Error(ex, "Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> Create([FromBody] string ProductName)
    {
        try
        {
            var result = await _productCategoryService.CreateAsync(ProductName);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryController").ForContext(_methodName, "Create").Error(ex, "Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> Update([FromBody] UpdateProductCategoryDto updatedProductCategory)
    {
        try
        {
            var result = await _productCategoryService.UpdateAsync(updatedProductCategory);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryController").ForContext(_methodName, "Update").Error(ex, "Error Occurred Invoking Endpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
