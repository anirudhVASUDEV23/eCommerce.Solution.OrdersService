using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroService.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroService.DataAccessLayer.Entities;
using eCommerce.OrdersMicroService.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;


public class OrdersService : IOrdersService
{
    private readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
    private readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
    private readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
    private readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;
    private readonly IMapper _mapper;
    private readonly IOrdersRepository _ordersRepository;
    private readonly UsersMicroServiceClient _usersMicroServiceClient;
    private readonly ProductsMicroServiceClient _productsMicroServiceClient;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(IOrdersRepository orders,IMapper mapper,IValidator<OrderAddRequest> orderAddRequestValidator,
        IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
        IValidator<OrderUpdateRequest> orderUpdateRequest,
        IValidator<OrderItemUpdateRequest> orderItemUpdateRequest,
        UsersMicroServiceClient usersMicroServiceClient,
        ProductsMicroServiceClient productsMicroServiceClient,
        ILogger<OrdersService> logger
    )
    {
        _orderAddRequestValidator = orderAddRequestValidator;
        _orderItemAddRequestValidator = orderItemAddRequestValidator;
        _orderUpdateRequestValidator = orderUpdateRequest;
        _orderItemUpdateRequestValidator = orderItemUpdateRequest;
        _ordersRepository = orders;
        _mapper = mapper;
        _usersMicroServiceClient = usersMicroServiceClient;
        _productsMicroServiceClient = productsMicroServiceClient;
        _logger = logger;
    }

    public async Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest)
    {
        if (orderAddRequest == null) throw new ArgumentNullException(nameof(orderAddRequest));
        //Validate the request
        ValidationResult validateResult= await _orderAddRequestValidator.ValidateAsync(orderAddRequest);
        if(!validateResult.IsValid)
        {
            string errors = string.Join(", ", validateResult.Errors.Select(temp => temp.ErrorMessage));
            throw new ArgumentException(errors);
        }

        List<ProductDTO?> products = new List<ProductDTO?>();

        //Validate order items using Fluent validation
        foreach(OrderItemAddRequest orderItemAddRequest in orderAddRequest.OrderItems)
        {
            ValidationResult validationResult = await _orderItemAddRequestValidator.ValidateAsync(orderItemAddRequest);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validateResult.Errors.Select(temp => temp.ErrorMessage));
                throw new ArgumentException(errors);
            }

            //TO DO:Logic for checking the product exists or not by calling the corresponding endpoint of the products microservice
            ProductDTO? product= await _productsMicroServiceClient.GetProductByProductID(orderItemAddRequest.ProductID);
            if (product == null)
            {
                throw new ArgumentException("ProductID is invalid");
            }
            else { _logger.LogInformation("Product ID is Valid!!!"); }
            products.Add(product);
            
        }

        //TO DO: Logic for checking the user id by calling the corresponding endpoint of the users microservice
        UserDTO? user= await _usersMicroServiceClient.GetUserByUserID(orderAddRequest.UserID);
        if(user==null)
        {
            throw new ArgumentException("UserID is invalid");
        }

        //convert data to order type using mapper
        Order orderInput= _mapper.Map<Order>(orderAddRequest);

        //total price for each order item and total bill
        foreach(OrderItem orderItem in orderInput.OrderItems)
        {
            orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
        }


        orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);


        //Invoke repository
        Order? addedOrder=  await _ordersRepository.AddOrder(orderInput);
        if (addedOrder == null) return null;
        OrderResponse addedOrderResponse = _mapper.Map<OrderResponse>(addedOrder);

        if (addedOrderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in addedOrderResponse.OrderItems)
            {
                ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }



        //TO DO: Load UserPersonName and Email from Users Microservice
        if (addedOrderResponse != null)
        {
            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, addedOrderResponse);
            }
        }

        return addedOrderResponse;
    }

    public async Task<bool> DeleteOrder(Guid orderID)
    {
        //check if the orderID exists
        FilterDefinition<Order> filter= Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);
        Order? existingOrder= await _ordersRepository.GetOrderByCondition(filter);
        if (existingOrder == null) return false;
        return await _ordersRepository.DeleteOrder(orderID);

    }

    public async Task<OrderResponse?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        Order? order = await _ordersRepository.GetOrderByCondition(filter);
        if (order == null)
            return null;

        OrderResponse orderResponse = _mapper.Map<OrderResponse>(order);


        //TO DO: Load ProductName and Category in OrderItem
        if (orderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroServiceClient.GetProductByProductID(orderItemResponse.ProductID);

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }


        //TO DO: Load UserPersonName and Email from Users Microservice
        if (orderResponse != null)
        {
            UserDTO? user = await _usersMicroServiceClient.GetUserByUserID(orderResponse.UserID);
            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, orderResponse);
            }
        }

        return orderResponse;
    }

    public async Task<List<OrderResponse?>> GetOrders()
    {
        IEnumerable<Order?> orders = await _ordersRepository.GetOrders();
        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);
        //TO DO:Load ProductName and Categpry in each OrderItem
        foreach(OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null) continue;
            foreach(OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroServiceClient.GetProductByProductID(orderItemResponse.ProductID);
                if (productDTO == null) continue;
                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO,orderItemResponse);
            }
            //TO DO: Load UserPersonName and Email from Users Microservice
            UserDTO? user= await _usersMicroServiceClient.GetUserByUserID(orderResponse.UserID);
            if(user!=null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user,orderResponse);
            }
        }
       
        return orderResponses.ToList();
    }

    public async Task<List<OrderResponse?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByCondition(filter);
        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);
        //TO DO:Load ProductName and Categpry in each OrderItem
        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null) continue;
            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroServiceClient.GetProductByProductID(orderItemResponse.ProductID);
                if (productDTO == null) continue;
                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
            //TO DO: Load UserPersonName and Email from Users Microservice
            UserDTO? user = await _usersMicroServiceClient.GetUserByUserID(orderResponse.UserID);
            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, orderResponse);
            }
        }
        return orderResponses.ToList();
    }

    public async Task<OrderResponse?> UpdateOrder(OrderUpdateRequest orderUpdateRequest)
    {
        if (orderUpdateRequest == null) throw new ArgumentNullException(nameof(orderUpdateRequest));
        //Validate the request
        ValidationResult validateResult = await _orderUpdateRequestValidator.ValidateAsync(orderUpdateRequest);
        if (!validateResult.IsValid)
        {
            string errors = string.Join(", ", validateResult.Errors.Select(temp => temp.ErrorMessage));
            throw new ArgumentException(errors);
        }

        List<ProductDTO> products = new List<ProductDTO>();

        //Validate order items using Fluent validation
        foreach (OrderItemUpdateRequest orderItemUpdateRequest in orderUpdateRequest.OrderItems)
        {
            ValidationResult validationResult = await _orderItemUpdateRequestValidator.ValidateAsync(orderItemUpdateRequest);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validateResult.Errors.Select(temp => temp.ErrorMessage));
                throw new ArgumentException(errors);
            }

            //TO DO:Logic for checking the product exists or not by calling the corresponding endpoint of the products microservice
            ProductDTO? product = await _productsMicroServiceClient.GetProductByProductID(orderItemUpdateRequest.ProductID);
            if (product == null)
            {
                throw new ArgumentException("ProductID is invalid");
            }
        }

        //TO DO: Logic for checking the user id by calling the corresponding endpoint of the users microservice
        UserDTO? user = await _usersMicroServiceClient.GetUserByUserID(orderUpdateRequest.UserID);
        if (user == null)
        {
            throw new ArgumentException("UserID is invalid");
        }

        //convert data to order type using mapper
        Order orderInput = _mapper.Map<Order>(orderUpdateRequest);

        //total price for each order item and total bill
        foreach (OrderItem orderItem in orderInput.OrderItems)
        {
            orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
        }


        orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);


        //Invoke repository
        Order? updatedOrder = await _ordersRepository.UpdateOrder(orderInput);
        if (updatedOrder == null) return null;
        OrderResponse updatedOrderResponse = _mapper.Map<OrderResponse>(updatedOrder); //Map updatedOrder ('Order' type) into 'OrderResponse' type (it invokes OrderToOrderResponseMappingProfile).


        //TO DO: Load ProductName and Category in OrderItem
        if (updatedOrderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in updatedOrderResponse.OrderItems)
            {
                ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }


        //TO DO: Load UserPersonName and Email from Users Microservice
        if (updatedOrderResponse != null)
        {
            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, updatedOrderResponse);
            }
        }

        return updatedOrderResponse; 
    }
}