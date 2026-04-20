using Microsoft.AspNetCore.Mvc;

namespace ProductManagementSystem.Api.Controllers.ServiceFilters;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{

    [HttpGet("_healths")]
    public async Task<IActionResult> GetHealthStatus()
    {
        return Redirect("admin/_healths");
    }
}
