namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record OrderItemResponse(Guid ProductID, decimal UnitPrice, int Quantity, decimal TotalPrice, string? ProductName, string? Category)
{//ProductName and Category has been added
    public OrderItemResponse() : this(default, default, default, default, default, default)
    {
    }
}