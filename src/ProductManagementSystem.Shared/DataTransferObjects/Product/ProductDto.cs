using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagementSystem.Shared.DataTransferObjects.Product;

public record class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string CategoryName { get; set; }
    public int CurrentCount { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
}
