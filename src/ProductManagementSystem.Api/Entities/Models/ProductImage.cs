namespace ProductManagementSystem.Api.Entities.Models;

public class ProductImage
{
    public int Id { get; set; }
    public string Filename { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    //RELATIONSHIP
    //Relationship - Product
    public int ProductId { get; set; }
    public Product product { get; set; }
}

