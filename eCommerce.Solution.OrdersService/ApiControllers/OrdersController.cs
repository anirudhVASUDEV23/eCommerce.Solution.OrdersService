using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroService.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroService.DataAccessLayer.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace OrdersMicroService.API.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersService _ordersService;

        public OrdersController(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        //GET: /api/orders
        [HttpGet]
        public async Task<IEnumerable<OrderResponse?>> Get()
        {
            List<OrderResponse?> orders= await _ordersService.GetOrders();
            return orders;
        }

        //GET: /api/orders/search/orderID/{orderID}
        [HttpGet("search/orderid/{orderID}")]
        public async Task<OrderResponse?> GetOrderByOrderID(Guid orderID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);
            OrderResponse? order = await _ordersService.GetOrderByCondition(filter);
            return order;
        }


        //GET: /api/orders/search/productid/{productID}
        [HttpGet("search/productid/{productID}")]
        public async Task<IEnumerable<OrderResponse?>> GetOrderByProductID(Guid productID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.ElemMatch(temp => temp.OrderItems,
                Builders<OrderItem>.Filter.Eq(tempProduct => tempProduct.ProductID, productID)
                );
            List<OrderResponse?> orders = await _ordersService.GetOrdersByCondition(filter);
            return orders;
        }


        //GET: /api/orders/search/orderDate/{orderDate}
        [HttpGet("search/orderDate/{orderDate}")]
        public async Task<IEnumerable<OrderResponse?>> GetOrderByOrderDate(DateTime orderDate)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(
                temp=>temp.OrderDate.ToString("yyyy-MM-dd"),orderDate.ToString("yyyy-MM-dd")
                );
            List<OrderResponse?> orders = await _ordersService.GetOrdersByCondition(filter);
            return orders;
        }

        ///GET: /api/orders/search/userid/{userID}
        [HttpGet("search/userid/{userID}")]
        public async Task<IEnumerable<OrderResponse?>> GetOrderByUserID(Guid userID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(
                temp => temp.UserID,userID
                );
            List<OrderResponse?> orders = await _ordersService.GetOrdersByCondition(filter);
            return orders;
        }


        [HttpPost()]
        public async Task<IActionResult> Post(OrderAddRequest orderAddRequest)
        {
            if (orderAddRequest == null) return BadRequest("Invalid order Data");
            OrderResponse? orderResponse= await _ordersService.AddOrder(orderAddRequest);
            if (orderResponse == null) return Problem("Error in adding Product");
            return Created($"api/Orders/search/orderid/{orderResponse?.OrderID}",orderResponse);
        }

        [HttpPut("{orderID}")]
        public async Task<IActionResult> Put(Guid orderID,OrderUpdateRequest orderUpdateRequest)
        {
            if (orderUpdateRequest == null) return BadRequest("Invalid order Data");
            if(orderID!=orderUpdateRequest.OrderID)
            {
                return BadRequest("OrderID in the url doesnt match with the order id in the request body");
            }
            OrderResponse? orderResponse = await _ordersService.UpdateOrder(orderUpdateRequest);
            if (orderResponse == null) return Problem("Error in adding Product");
            return Ok(orderResponse);
        }

        [HttpDelete("{orderID}")]
        public async Task<IActionResult> Delete(Guid orderID)
        {
            if (orderID == Guid.Empty) return BadRequest("Invalid order ID");
            bool result = await _ordersService.DeleteOrder(orderID);
            if (!result) return Problem("Error in deleting the product");
            return Ok(result);
        }



    }
}
