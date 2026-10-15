namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record OrderResponse(Guid OrderID, Guid UserID, decimal TotalBill, DateTime OrderDate, List<OrderItemResponse> OrderItems,string? UserPersonName,string? Email)//UserPersonName and Email added newly we copy this into OrderResponse by mapping
{
    public OrderResponse() : this(default, default, default, default, default,default,default)
    {
    }
}