using Store.Entities;

namespace Store;

public class CartItem
{
    public Product Product { get; init; } = null!;
    public int Quantity { get; set; }
    public decimal PricePerUnit => Product.Price;
    public decimal Total => Quantity * PricePerUnit;
}

public class TopClientReportItem
{
    public string FullName { get; init; } = null!;
    public int OrdersCount { get; init; }
    public decimal TotalSpent { get; init; }
}