namespace ISM_BACKEND.Models;

public class CreateProductUnitRequest
{
    public string name { get; set; } = "";
}

public class ProductUnitItem
{
    public string name { get; set; } = "";
}

public class CreateProductRequest
{
    public string productNo { get; set; } = "";
    public string name { get; set; } = "";
    public string unit { get; set; } = "";
    public decimal price { get; set; }
}

public class ProductItem
{
    public long productId { get; set; }
    public string productNo { get; set; } = "";
    public string name { get; set; } = "";
    public string unit { get; set; } = "";
    public decimal price { get; set; }
}
