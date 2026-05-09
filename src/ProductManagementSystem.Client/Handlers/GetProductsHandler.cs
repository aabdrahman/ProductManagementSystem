using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetProductsHandler
{
    private readonly HttpClient _httpClient;

    public GetProductsHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(IEnumerable<ProductDto> products, string message)> Handle()
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync("api/product");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<ProductDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<ProductDto>>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? throw new ArgumentException("Response could not be deserialized.");

            if(responseBody.IsSuccessStatus && responseBody.Data.Any())
            {
                var sampleResp = responseBody.Data.ToList();

                responseBody.Data = responseBody.Data.Select(x => x with { ProductImages = x.ProductImages.Select(y => string.Concat(_httpClient.BaseAddress, $"api/ProductImage?productId={x.Id}&productImageName={y}")).ToList() });

                

                //var responseDetails = responseBody.Data.Select(x => x.ProductImages.Select(y => string.Concat(_httpClient.BaseAddress, $"api/ProductImage?productId={x.Id}&productImageName={y}"))).ToList();

                //Console.WriteLine("Products; {0}", JsonSerializer.Serialize(respDetails));

                return (responseBody.Data, responseBody.ResponseMessage);
            }

            return (responseBody.Data ?? [], responseBody.ResponseMessage);

        }
        catch (Exception ex)
        {

            return([], ex.Message);
        }
    }
}
