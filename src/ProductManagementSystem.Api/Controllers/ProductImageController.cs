using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Controllers.ServiceFilters;
using ProductManagementSystem.Api.Services.Contracts;
using Serilog;
using System.Security.Cryptography;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SYSTEM,ADMIN")]
public class ProductImageController : ControllerBase
{
    private readonly IProductImageService _productImageService;

    private Serilog.ILogger logger;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public ProductImageController(IProductImageService productImageService)
    {
        _productImageService = productImageService;

        logger = Log.ForContext(_className, nameof(ProductImageController));
    }

    [HttpPost("{productId:int}")]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> ProcessImage([FromRoute] int productId)
    {
        var logProvider = logger.ForContext(_methodName, nameof(ProcessImage));
        try
        {
            var imagesCollection = Request.Form.Files;

            if(imagesCollection == null || imagesCollection.Count == 0)
            {
                return BadRequest("No image provided.");
            }

            var isProcessed = await _productImageService.AddProductImage(productId, imagesCollection.Select(x => x).ToList());

            if (isProcessed)
            {
                return Ok("Fiels processed successfullly.");
            }

            return BadRequest("Files could not be processed.");
            
        }
        catch (Exception ex)
        {
            logProvider.Error(ex, "An error occrred invoking endpoint.");

            return BadRequest(ex.Message);
        }
    }

    [HttpDelete]
    [ServiceFilter(typeof(AuthenticationTokenValidationFilter))]
    public async Task<IActionResult> RemoveImage([FromQuery] int productId,  [FromQuery] string productImageName)
    {
        var logProvider = logger.ForContext(_methodName, nameof(RemoveImage));
        try
        {
            var isRemoved = await _productImageService.RemoveProductImage(productId, productImageName);

            return isRemoved ? Ok("Image removed successfully.") : BadRequest("Image could not be removed.");
        }
        catch (Exception ex)
        {
            logProvider.Error(ex, "An error occurred invoking endpoint.");
            return BadRequest(ex.Message);
            
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetFiles([FromQuery] int productId, [FromQuery] string productImageName)
    {
        var logProvider = logger.ForContext(_methodName, nameof(GetFiles));
        try
        {
            var fileDetails = await _productImageService.GetProductImage(productId, productImageName);

            return File(fileDetails, "image/png");
        }
        catch (Exception ex)
        {
            logProvider.Error(ex, "An error occurred invoking endpoint.");
            return BadRequest(ex.Message);
        }
    }
}
