using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Product;

public record class CreateProductDto : IValidatableObject
{
    [Required(ErrorMessage = "Product Name is a required field")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; }
    [Required(ErrorMessage = "Brief Product Desctiption is a required field")]
    [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
    public string? Description { get; set; }
    [Required(ErrorMessage = "Category is a required field")]
    [Range(1, double.MaxValue, ErrorMessage = "Provide appropriate Category")]
    public int CategoryId { get; set; }
    [Required(ErrorMessage = "Stock Count is a required field")]
    [Range(1, double.MaxValue, ErrorMessage = "Stock Count cannot be less tahn 1")]
    public int CurrentCount { get; set; }
    [Required(ErrorMessage = "Cost Price is a required field")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Cost Price cannot be lesser than 0.01")]
    public decimal CostPrice { get; set; }
    [Required(ErrorMessage = "Selling Price is a required field")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Selling Price cannot be lesser than 0.01")]
    public decimal SellingPrice { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Name) || Name.Length > 100 || Name.Length < 2)
        {
            yield return new ValidationResult("Name is a required field and must be between 2 and 100 characters");
        }

        if (CostPrice < 0.01m)
        {
            yield return new ValidationResult("Cost Price must be greater than 0");
        }

        if (SellingPrice < 0.01m)
        {
            yield return new ValidationResult("Selling Price must be greater than 0");
        }

        if (CurrentCount < 0)
        {
            yield return new ValidationResult("Count must be greater than 0");
        }

        if (CategoryId <= 0)
        {
            yield return new ValidationResult("Category Id must be greater than 0");
        }
    }
}

