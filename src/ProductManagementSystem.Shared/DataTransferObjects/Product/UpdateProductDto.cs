using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Product;

public record class UpdateProductDto : IValidatableObject
{
    [Required(ErrorMessage = "Id is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "Kindly provide an appropriate product Id")]
    public int Id { get; set; }
    [Required(ErrorMessage = "Id is a required field.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; }
    [StringLength(250, ErrorMessage = "Name cannot exceed 250 characters")]
    public string? Description { get; set; }
    [Required(ErrorMessage = "Category Id is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "Kindly provide an appropriate category Id")]
    public int CategoryId { get; set; }
    [Required(ErrorMessage = "Current Count is a required field.")]
    public int CurrentCount { get; set; }
    [Required(ErrorMessage = "Cost Price is a required field.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Kindly provide an accurate cost price")]
    public decimal CostPrice { get; set; }
    [Required(ErrorMessage = "Selling Price is a required field.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Kindly provide an accurate selling price")]
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

