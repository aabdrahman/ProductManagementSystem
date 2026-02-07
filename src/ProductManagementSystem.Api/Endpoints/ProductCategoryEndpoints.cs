using ProductManagementSystem.Api.Services.Contracts;

namespace ProductManagementSystem.Api.Endpoints;

public static class ProductCategoryEndpoints
{
    public static RouteGroupBuilder MapProductCategoryEndpoints(this WebApplication app)
    {
        RouteGroupBuilder endPointGroup = app.MapGroup("ProductCategory");

        endPointGroup.MapGet("/", async (IProductCategoryService productCategoryService) =>
        {
            var result = await productCategoryService.GetAllAsync();

            return result.IsSuccessStatus ? 
                    Results.Ok(result) :
                    Results.BadRequest(result);
            ;

            //return Results.StatusCode(result.StatusCode);
        });

        return endPointGroup;
    }
}
