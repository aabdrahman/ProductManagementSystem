using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetProductHandler
{
    private readonly HttpClient _httpClient;

    public GetProductHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("Api Key cannot be found."));
    }

    public async Task<(ProductDto? product, string responseMessage)> Handle(int Id)
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync($"api/product/{Id}");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<ProductDto> responseBody = JsonSerializer.Deserialize<GenericResponse<ProductDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? throw new ArgumentNullException("Response could not be deserialized");

            if (responseBody.IsSuccessStatus && responseBody.Data is not null)
            {
                var productId = responseBody.Data.Id;
                var productImages = responseBody.Data.ProductImages.Select(x => string.Concat(_httpClient.BaseAddress.ToString(), $"api/ProductImage?productId={productId}&productImageName={x}")).ToList();

                responseBody.Data.ProductImages = productImages;

                Console.WriteLine("Returned value: {0}", JsonSerializer.Serialize(responseBody.Data));

                return (responseBody.Data, responseBody.ResponseMessage);



            }

            return (responseBody.Data, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
