using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Product;

public record class UpdateProductDto : IValidatableObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int CurrentCount { get; set; }
    public decimal CostPrice { get; set; }
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

        if(Id < 1)
        {
            yield return new ValidationResult("Product Id must be greater than 0");
        }
    }
}

