using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Product;

public record class UpdateProductStockDto
{
    [Required(ErrorMessage = "Product Id is a required field.")]
    public int Id { get; set; }
    [Required(ErrorMessage = "Stock To Add is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "Stock To Add cannot be less than 1.")]
    public int StockToAdd { get; set; }
}
