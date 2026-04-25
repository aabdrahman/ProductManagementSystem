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
            Console.WriteLine("Begin Handler operation...");
            var filePath = Path.Combine(_env.WebRootPath, "Images", uploadedFile.Name);
            Console.WriteLine($"File path: {filePath}");

            //var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

            using var ms = new MemoryStream();
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                Console.WriteLine("Begin stream operation...");
                var buffer = new byte[1024 * 16];
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    // Write the chunk into our MemoryStream
                    await ms.WriteAsync(buffer.AsMemory(0, bytesRead));
                }

                await fileStream.CopyToAsync(ms);

                Console.WriteLine("File copied...");

                //await fileStream.WriteAsync(buffer);

                await fileStream.DisposeAsync();
            }
            Console.WriteLine("File stream disposed...");
            return (true, "File Uploaded Successfully.");
        }
		catch (Exception ex)
		{
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return (false, ex.Message);
		}
    }
}
