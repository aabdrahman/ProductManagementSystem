using Microsoft.AspNetCore.Components.Forms;

namespace ProductManagementSystem.Client.Handlers;

public class AddHeroImageHandler
{
	private readonly IWebHostEnvironment _env;

    public AddHeroImageHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(IBrowserFile uploadedFile, string fileName)
    {
		try
		{
            var filePath = Path.Combine(_env.WebRootPath, "Images", uploadedFile.Name);

            //var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                var buffer = new byte[uploadedFile.Size];

                await uploadedFile.OpenReadStream(uploadedFile.Size).ReadAsync(buffer);

                await fileStream.WriteAsync(buffer);

                await fileStream.DisposeAsync();
            }

            return (true, "File Uploaded Successfully.");
        }
		catch (Exception ex)
		{
            Console.WriteLine(ex.ToString());
            return (false, ex.Message);
		}
    }
}
