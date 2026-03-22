using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.MailOperation;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;

namespace ProductManagementSystem.Api.Services;

public class OrderService : IOrderService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly IEmailVerificationLinkFactory _emailVerificationLinkFactory;
    private readonly IEmailService _emailService;
    private readonly UserOrderVerificationConfig _userOtpVerificationConfig;

    public OrderService(RepositoryContext repositoryContext, IEmailVerificationLinkFactory emailVerificationLinkFactory, IEmailService emailService, IOptionsMonitor<UserOrderVerificationConfig> optionsMonitor)
    {
        _repositoryContext = repositoryContext;
        _emailVerificationLinkFactory = emailVerificationLinkFactory;
        _emailService = emailService;
        _userOtpVerificationConfig = optionsMonitor.CurrentValue;
    }

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public Task<GenericResponse<string>> CancelOrderAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public async Task<GenericResponse<OrderDto>> CreateAsync(CreateOrderDto createOrder)
    {
        try
        {
            Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Create Order - {orderToCreate}", JsonSerializer.Serialize(createOrder));

            //Validate the product to order exists and the quantity ordered is available in stock before creating the order

            List<int> productIds = createOrder.OrderLineItems.Select(x => x.ProductId).ToList();

            List<Product> productsToOrder = await _repositoryContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

            if(productIds.Count != productsToOrder.Count)
            {
                var existingProductIds = productsToOrder.Select(x => x.Id);
                var nonExistingProductIds = productIds.Except(existingProductIds);
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("The following product Ids do not exist: {nonExistingProductIds}", JsonSerializer.Serialize(nonExistingProductIds));
                return GenericResponse<OrderDto>.Failure(null, $"The following product Ids do not exist: {JsonSerializer.Serialize(nonExistingProductIds)}", System.Net.HttpStatusCode.NotFound);
            }

            var joinedOrderItems = createOrder.OrderLineItems.Join(productsToOrder, oli => oli.ProductId, p => p.Id, (oli, p) => new { OrderLineItem = oli, Product = p, StockAvailable = p.CurrentCount >= oli.QuantityOrdered }).ToList();

            if(joinedOrderItems.Any(x => !x.StockAvailable))
            {
                var outOfStockProducts = joinedOrderItems.Where(x => !x.StockAvailable).Select(x => new { x.Product.Id, x.Product.NormalizedName, x.Product.CurrentCount, x.OrderLineItem.QuantityOrdered });
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("The following products are out of stock: {outOfStockProducts}", JsonSerializer.Serialize(outOfStockProducts));
                return GenericResponse<OrderDto>.Failure(null, $"The following products are out of stock: {JsonSerializer.Serialize(outOfStockProducts.Select(x => x.NormalizedName).ToList())}", System.Net.HttpStatusCode.BadRequest);
            }

            //DISABLED AS THE PRODUCT ID IS NOT NEEDED IN THE ORDER ENTITY. THE PRODUCT ID IS REFERENCED IN THE ORDER LINE ITEM ENTITY AND THE ORDER CAN HAVE MULTIPLE PRODUCTS THROUGH THE ORDER LINE ITEM COLLECTION

            //Product? productToOrder = await _repositoryContext.Products.SingleOrDefaultAsync(x => x.Id == createOrder.ProductId);

            //if(productToOrder is null)
            //{
            //    Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("No Product Found - {Id}", createOrder.ProductId);
            //    return GenericResponse<OrderDto>.Failure(null, $"The provided product Id does not exist: {createOrder.ProductId}", System.Net.HttpStatusCode.NotFound);
            //}

            //if(productToOrder.CurrentCount < createOrder.QuantityOrdered)
            //{
            //    Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Product is out of stock. Current Count: {currCount}. Ordered Count: {orderCount}", productToOrder.CurrentCount, createOrder.QuantityOrdered);
            //    return GenericResponse<OrderDto>.Failure(null, $"Selected Product is out of stock. Available Quantity: {productToOrder.CurrentCount}", System.Net.HttpStatusCode.BadRequest);
            //}

            //productToOrder.CurrentCount -= createOrder.QuantityOrdered;


            //TO REDUCE COUNT OF THE PRODUCTS IN THE ORDER, LOOP THROUGH THE ORDER LINE ITEMS AND REDUCE THE COUNT OF EACH PRODUCT IN THE ORDER LINE ITEM FROM THE PRODUCT TABLE
            foreach(var orderLineItem in joinedOrderItems)
            {
                orderLineItem.Product.CurrentCount -= orderLineItem.OrderLineItem.QuantityOrdered;
            }

            //DO NOT UNCOMMENT TO PREVENT MULTIPLE STOCK COUNT REDUCTION(THIS WILL BE DELETED LATER) !!IMPORTANT!!
            //foreach (var product in productsToOrder)
            //{
            //    int productCount = createOrder.OrderLineItems.Where(x => x.ProductId == product.Id).Select(x => x.QuantityOrdered).FirstOrDefault();

            //    product.CurrentCount -= productCount;
            //}

            //Create an instance of the order to insert from the order from request
            Order orderToInsert = new Order()
            {
                CreatedBy = createOrder.CreatedBy,
                //OrderCount = createOrder.QuantityOrdered,
                //OrderedProduct = productToOrder,
                OrderStatus = Entities.StaticValues.OrderStatus.Pending,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                DeliveryAddress = createOrder.DeliveryAddress,
                OrderTrackingId = GetOrderTrackingId()
            };

            //Create order line items from the order line items from request and set to the order line items collection of the order to insert
            List<OrderLineItem> orderLineItems = createOrder.OrderLineItems.Select(x => new OrderLineItem()
            {

                ProductId = x.ProductId,
                QuantityOrdered = x.QuantityOrdered

            }).ToList();

            orderToInsert.OrderLineItems = orderLineItems;
            orderToInsert.UserOrderVerificationTokens.Add(new UserOrderVerificationToken()
            {
                VerificationToken = GetOrderVerificationToken()
            });

            //Add the order to the database and update the product count for the ordered product
            await _repositoryContext.Orders.AddAsync(orderToInsert);
            //_repositoryContext.Products.Update(productToOrder);

            await _repositoryContext.SaveChangesAsync();

            try
            {
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Getting and sending email for order verification....");
                string verificationLink = _emailVerificationLinkFactory.GetEmailVerificationLink(orderToInsert.UserOrderVerificationTokens.First());

                Dictionary<string, string> emailContentParameters = new Dictionary<string, string>();
                emailContentParameters.Add("VERIFICATION_URL", verificationLink);
                emailContentParameters.Add("ORDER_NUMBER", orderToInsert.OrderTrackingId);
                emailContentParameters.Add("VERIFICATION_EXPIRE", _userOtpVerificationConfig.ExpiresAfterInMinutes > 60 ? $"{(_userOtpVerificationConfig.ExpiresAfterInMinutes / 60)} hour(s) {_userOtpVerificationConfig.ExpiresAfterInMinutes % 60} minutes" : 
                                                                $"{_userOtpVerificationConfig.ExpiresAfterInMinutes.ToString()}");

                string emailContent = EmailContentHelper.GetMailContent("ConfirmOrder.html", emailContentParameters);

                EmailSenderDto verificationEmail = new EmailSenderDto(Subject: "Order Confirmation Notification", Content: emailContent, Recipients: [orderToInsert.CreatedBy], isHtml: true);

                bool isEmailQueued = await _emailService.SendEmailAsync(verificationEmail);

                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Order Verification Link generated Successfully. Queue Notification Status - {0}", isEmailQueued);

            }
            catch (Exception ex)
            {
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Error(ex, "An Error Occurred Generating and sending order verification link.");
            }

            OrderDto orderCreated = new OrderDto()
            {
                CreatedBy = orderToInsert.CreatedBy,
                Id = orderToInsert.Id,
                OrderStatus = orderToInsert.OrderStatus.ToString(),
                CreatedDate = orderToInsert.CreatedAt,
                LineItemsCount = orderToInsert.OrderLineItems.Count,
                OrderNumber = orderToInsert.OrderTrackingId
                //Product = productToOrder.NormalizedName,
                //QuantityOrdered = orderToInsert.OrderCount
            };

            Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Order Successfully created - {createdOrder}", JsonSerializer.Serialize(orderCreated));

            return GenericResponse<OrderDto>.Success(orderCreated, "Order successfully created.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Error(ex, "Database error occurred creating order.");
            return GenericResponse<OrderDto>.Failure(null, "An error occurred inserting record into database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Error(ex, "Database error occurred creating order.");
            return GenericResponse<OrderDto>.Failure(null, "An error occurred creating order.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true)
    {
        try
        {
            Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Information("Deleting Created Order - {Id}. Parameter: {isSoftDelete}", Id, isSoftDelete);

            //Fetch the order to delete along with the ordered product details to enable modification of the product count for the ordered product when the order is deleted
            Order? order = await _repositoryContext.Orders.IgnoreQueryFilters().Include(x => x.OrderLineItems).ThenInclude(x => x.OrderedProduct).SingleOrDefaultAsync(x => x.Id == Id);

            if(order is null)
            {
                Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Information("Order does not exist - {Id}", Id);
                return GenericResponse<string>.Failure("Operation Failed.", $"Order does not exist for Id: {Id}.", System.Net.HttpStatusCode.NotFound);
            }

            //Loop through the order line items and increase the count of each product in the order line item by the quantity ordered for that product in the order line item
            foreach (var orderItem in order.OrderLineItems)
            {
                int quantityOrdered = orderItem.QuantityOrdered;
                orderItem.OrderedProduct.CurrentCount += quantityOrdered;
            }

            if (isSoftDelete)
            {
                order.OrderStatus = Entities.StaticValues.OrderStatus.Cancelled;
                order.IsActive = false;

                //order.OrderedProduct.CurrentCount += order.OrderCount;

                //_repositoryContext.Orders.Update(order);

            }
            else
            {
                //order.OrderedProduct.CurrentCount += order.OrderCount;

                //_repositoryContext.Products.Update(order.OrderedProduct);

                _repositoryContext.Orders.Remove(order);
            }

            await _repositoryContext.SaveChangesAsync();

            return GenericResponse<string>.Success("Operation Successful", $"{(isSoftDelete ? "Order successfully deactivated" : "Order successfully removed")}", System.Net.HttpStatusCode.OK);
        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Error(ex, "Database error occurred performing operation");
            return GenericResponse<string>.Failure("Operation Failed.", "Database error occurred.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Error(ex, "An error occurred performing operation");
            return GenericResponse<string>.Failure("Operation Failed.", "An error occurred.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<OrderDto>>> GetAllAsync()
    {
        try
        {
            Log.ForContext(_methodName, "GetAllAsync").ForContext(_className, "OrderService").Information("Fetching All Orders......");

            List<OrderDto> orders = await _repositoryContext.Orders
                                        .AsNoTracking()
                                        .Select(x => new OrderDto()
                                        {
                                            Id = x.Id,
                                            //QuantityOrdered = x.OrderCount,
                                            OrderStatus = x.OrderStatus.ToString(),
                                            //Product = x.OrderedProduct.NormalizedName,
                                            CreatedDate = x.CreatedAt.ToLocalTime(),
                                            CreatedBy = x.CreatedBy,
                                            LineItemsCount = x.OrderLineItems.Count,
                                            OrderNumber = x.OrderTrackingId
                                        })
                                        .ToListAsync();

            Log.ForContext(_methodName, "GetAllAsync").ForContext(_className, "OrderService").Information("Orders Fetched Successfully - {orders}", JsonSerializer.Serialize(orders));

            return GenericResponse<IEnumerable<OrderDto>>.Success(orders, "Order Fetched Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_methodName, "GetAllAsync").ForContext(_className, "OrderService").Error(ex, "Error fetching orders from database");
            return GenericResponse<IEnumerable<OrderDto>>.Failure(null, "Error occurred fetching orders from database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "GetAllAsync").ForContext(_className, "OrderService").Error(ex, "Error occurred fetching orders.");
            return GenericResponse<IEnumerable<OrderDto>>.Failure(null, "Error occurred fetching orders.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<OrderDto>> GetByIdAsync(int Id)
    {
        try
        {
            Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Information("Fetch Products by Id - {Id}", Id);

            OrderDto? order = await _repositoryContext.Orders
                                            .AsNoTracking()
                                            .Select(x => new OrderDto()
                                            {
                                                Id = x.Id,
                                                //QuantityOrdered = x.OrderCount,
                                                OrderStatus = x.OrderStatus.ToString(),
                                                //Product = x.OrderedProduct.NormalizedName,
                                                CreatedDate = x.CreatedAt.ToLocalTime(),
                                                CreatedBy = x.CreatedBy,
                                                LineItemsCount = x.OrderLineItems.Count,
                                                OrderNumber = x.OrderTrackingId
                                            })
                                            .SingleOrDefaultAsync(x => x.Id == Id);

            if(order is null)
            {
                Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Information("No Order with specified Id - {Id}", Id);
                return GenericResponse<OrderDto>.Failure(null, $"No order exists with Id: {Id}", System.Net.HttpStatusCode.NotFound);
            }

            Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Information("Order with Id: {Id} Fetched Successully - {order}", Id, JsonSerializer.Serialize(order));
            return GenericResponse<OrderDto>.Success(order, "Order Fetched Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Error(ex, "Error fetching record from database - {Id}", Id);
            return GenericResponse<OrderDto>.Failure(null, "Error occurred fetching order from database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Error(ex, "An Error fetching record - {Id}", Id);
            return GenericResponse<OrderDto>.Failure(null, "Error occurred fetching order.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<OrderDto>>> GetByProductIdAsync(int ProductId)
    {
        try
        {
            Log.ForContext(_methodName, "GetByProductIdAsync").ForContext(_className, "OrderService").Information("Fetching Orders for product - {productId}", ProductId);

            List<OrderDto> orders = await _repositoryContext.Orders
                                        .AsNoTracking()
                                        .Select(x => new OrderDto()
                                        {
                                            Id = x.Id,
                                            //QuantityOrdered = x.OrderCount,
                                            OrderStatus = x.OrderStatus.ToString(),
                                            //Product = x.OrderedProduct.NormalizedName,
                                            CreatedDate = x.CreatedAt.ToLocalTime(),
                                            CreatedBy = x.CreatedBy,
                                            LineItemsCount = x.OrderLineItems.Count,
                                            OrderNumber = x.OrderTrackingId
                                        })    
                                        .ToListAsync();

            if(!orders.Any())
            {
                Log.ForContext(_methodName, "GetByProductIdAsync").ForContext(_className, "OrderService").Information("Order does not exist for product - {productId}", ProductId);
                return GenericResponse<IEnumerable<OrderDto>>.Failure(null, $"No Order exists for specified product - {ProductId}", System.Net.HttpStatusCode.NotFound);
            }

            Log.ForContext(_methodName, "GetByProductIdAsync").ForContext(_className, "OrderService").Information("Orders fetched for Product: {Id} - {orders}", ProductId, JsonSerializer.Serialize(orders));

            return GenericResponse<IEnumerable<OrderDto>>.Success(orders, "Orders Fetched Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_methodName, "GetByProductIdAsync").ForContext(_className, "OrderService").Error(ex, "Error occurred fetching orders from database.");
            return GenericResponse<IEnumerable<OrderDto>>.Failure(null, "Error fetching orders form database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "GetByProductIdAsync").ForContext(_className, "OrderService").Error(ex, "Error occurred fetching orders.");
            return GenericResponse<IEnumerable<OrderDto>>.Failure(null, "Error fetching orders.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<OrderDto>> UpdateAsync(UpdateOrderDto updateOrder)
    {
        try
        {
            Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Information("Update Order Details - {orderDetails}", JsonSerializer.Serialize(updateOrder));

            Order? orderToUpdate = await _repositoryContext.Orders.IgnoreQueryFilters().Include(x => x.OrderLineItems).SingleOrDefaultAsync(x => x.Id == updateOrder.Id);

            if(orderToUpdate is null)
            {
                Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Information("Order with Id does not exist - {Id}", updateOrder.Id);
                return GenericResponse<OrderDto>.Failure(null, $"Order with Id: {updateOrder.Id} does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            if (orderToUpdate.DeliveryDate.HasValue || orderToUpdate.OrderStatus == Entities.StaticValues.OrderStatus.Cancelled)
            {
                Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Information("Order with Id: {Id} has an invalid status - {status}", updateOrder.Id, orderToUpdate.OrderStatus.ToString());
                return GenericResponse<OrderDto>.Failure(null, $"Cannot perform operation as order is already: {orderToUpdate.OrderStatus.ToString()}", System.Net.HttpStatusCode.Conflict);
            }

            //if(orderToUpdate.ProductId == updateOrder.ProductId && orderToUpdate.OrderCount == updateOrder.QuantityOrdered) //SAME ORDER PRODUCT AND THE QUANTITY DOES NOT CHANGE -- NO MODIFICATION TO PRODUCT COUNT
            //{
            //    Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Information("No modification to details");

            //    return GenericResponse<OrderDto>.Success(new OrderDto()
            //                                                {
            //                                                    Id = orderToUpdate.Id,
            //                                                    QuantityOrdered = orderToUpdate.OrderCount,
            //                                                    CreatedDate = orderToUpdate.CreatedAt,
            //                                                    CreatedBy = orderToUpdate.CreatedBy,
            //                                                    Product = orderToUpdate.OrderedProduct.NormalizedName,
            //                                                    OrderStatus = orderToUpdate.OrderStatus.ToString()
            //                                                }, 
            //                                                "Update Successful", System.Net.HttpStatusCode.OK);
            //}
            //else if(orderToUpdate.ProductId == updateOrder.ProductId && orderToUpdate.OrderCount != updateOrder.QuantityOrdered)
            //{
            //    orderToUpdate.OrderedProduct.CurrentCount += orderToUpdate.OrderCount;

            //    if(orderToUpdate.OrderedProduct.CurrentCount < updateOrder.QuantityOrdered)
            //    {
            //        Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Information("The available stock: {currentStock} for product: {Id} cannot accommodate requested quantity: {requestedCount}", orderToUpdate.OrderedProduct.CurrentCount, orderToUpdate.ProductId, updateOrder.QuantityOrdered);
            //        return GenericResponse<OrderDto>.Failure(null, $"The current product stock: {orderToUpdate.OrderedProduct.CurrentCount} cannot accommodate requested quantity: {updateOrder.QuantityOrdered}.", System.Net.HttpStatusCode.Conflict);
            //    }

            //    orderToUpdate.OrderedProduct.CurrentCount -= updateOrder.QuantityOrdered;
            //}
            //else if(orderToUpdate.ProductId != updateOrder.ProductId)
            //{

            //    Product? productToOrder = await _repositoryContext.Products.SingleOrDefaultAsync(x => x.Id == updateOrder.ProductId);

            //    if(productToOrder is null)
            //    {
            //        Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Information("The provided product id does not exist - {Id}", updateOrder.ProductId);
            //        return GenericResponse<OrderDto>.Failure(null, $"No product exists with specified Id: {updateOrder.ProductId}", System.Net.HttpStatusCode.NotFound);
            //    }

            //    if(productToOrder.CurrentCount < updateOrder.QuantityOrdered)
            //    {
            //        Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Information("The available stock: {currentStock} for product: {Id} cannot accommodate requested quantity: {requestedCount}", productToOrder.CurrentCount, updateOrder.ProductId, updateOrder.QuantityOrdered);
            //        return GenericResponse<OrderDto>.Failure(null, $"The current product stock: {productToOrder.CurrentCount} cannot accommodate requested quantity: {updateOrder.QuantityOrdered}.", System.Net.HttpStatusCode.Conflict);
            //    }

            //    var previousProduct = orderToUpdate.OrderedProduct;
            //    previousProduct.CurrentCount += orderToUpdate.OrderCount;

            //    productToOrder.CurrentCount -= updateOrder.QuantityOrdered;

            //    orderToUpdate.OrderCount = updateOrder.QuantityOrdered;
            //    orderToUpdate.ProductId = updateOrder.ProductId;
            //    orderToUpdate.OrderedProduct = productToOrder;

            //    //_repositoryContext.Products.Update(previousProduct);
            //}

            orderToUpdate.DeliveryAddress = updateOrder.DeliveryAddress;

            //_repositoryContext.Orders.Update(orderToUpdate);

            await _repositoryContext.SaveChangesAsync();

            OrderDto orderUpdated = new OrderDto()
            {
                Id = orderToUpdate.Id,
                //QuantityOrdered = orderToUpdate.OrderCount,
                CreatedDate = orderToUpdate.CreatedAt.ToLocalTime(),
                CreatedBy = orderToUpdate.CreatedBy,
                //Product = orderToUpdate.OrderedProduct.NormalizedName,
                OrderStatus = orderToUpdate.OrderStatus.ToString(),
                OrderNumber = orderToUpdate.OrderTrackingId,
                LineItemsCount = orderToUpdate.OrderLineItems.Count
            };

            return GenericResponse<OrderDto>.Success(orderUpdated, "Order Updated Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Error(ex, "A database Error Occurred Updating Details.");
            return GenericResponse<OrderDto>.Failure(null, "Database Error Occurred updating the order details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Error(ex, "An Error Occurred Updating Details.");
            return GenericResponse<OrderDto>.Failure(null, "An Error Occurred updating the order details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> UpdateOrderStatusAsync(UpdateOrderStatusDto updateOrderStatus)
    {
        try
        {
            Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Information("Update Order Stattus - {orderOperationDetails}", JsonSerializer.Serialize(updateOrderStatus));

            Order? orderToUpdate = await _repositoryContext.Orders.Include(x => x.OrderLineItems).SingleOrDefaultAsync(x => x.Id == updateOrderStatus.Id);

            if(orderToUpdate is null)
            {
                Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Information("Order with record does not exist - {Id}", updateOrderStatus.Id);
                return GenericResponse<string>.Failure(null, $"Order with specified Id: {updateOrderStatus.Id} does not exist", System.Net.HttpStatusCode.NotFound);
            }

            if(orderToUpdate.OrderStatus == Entities.StaticValues.OrderStatus.Delivered || orderToUpdate.OrderStatus == Entities.StaticValues.OrderStatus.Cancelled)
            {
                Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Information("Order is already {orderToUpdateStatus}", orderToUpdate.OrderStatus.ToString());
                return GenericResponse<string>.Failure("Operation Failed.", $"Order is already: {orderToUpdate.OrderStatus.ToString()}", System.Net.HttpStatusCode.Conflict);
            }
            
            if(!Enum.TryParse(typeof(Entities.StaticValues.OrderStatus), updateOrderStatus.UpdatedStatus, true, out var result))
            {
                Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Information("Invalid Order Status Provided - {orderStatus}", updateOrderStatus.UpdatedStatus);
                return GenericResponse<string>.Failure("Operation Failed.", "Invalid Status To Update", System.Net.HttpStatusCode.BadRequest);
            }

            orderToUpdate.OrderStatus = Enum.Parse<Entities.StaticValues.OrderStatus>(updateOrderStatus.UpdatedStatus, ignoreCase: true);

            if(orderToUpdate.OrderStatus == Entities.StaticValues.OrderStatus.Delivered)
            {
                orderToUpdate.DeliveryDate = DateTime.UtcNow;

                Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Information("Order with Id: {Id} has been delivered. Delivery date set to {deliveryDate}. Then, order line item products are increased", orderToUpdate.Id, orderToUpdate.DeliveryDate);
            }

            //_repositoryContext.Orders.Update(orderToUpdate);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Information("Order Status Successfully updated - {updatedSttaus}", orderToUpdate.OrderStatus.ToString());

            return GenericResponse<string>.Success("Operation Successful", "Order Status Updated Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Error(ex, "A Database Error Occurred Updating Order Details");
            return GenericResponse<string>.Failure("Operation Failed.", "A Database Error Occurred Updating Order Details", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });    
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "UpdateOrderStatusAsync").ForContext(_className, "OrderService").Error(ex, "An Error Occurred Updating Order Details.");
            return GenericResponse<string>.Failure("Operation Failed.", "An Error Occurred Updating Order Details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<OrderDetailsDto>> GetOrderDetailsAsync(int OrderId)
    {
        try
        {
            Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Information("Fetching Order Details for Order - {Id}", OrderId);

            OrderDetailsDto? orderDetails = await _repositoryContext.Orders
                                        .AsNoTracking()
                                        .Where(x => x.Id == OrderId)
                                        .Select(x => new OrderDetailsDto()
                                        {
                                            Id = x.Id,
                                            CreatedBy = x.CreatedBy,
                                            CreatedDate = x.CreatedAt.ToLocalTime(),
                                            DeliveryAddress = x.DeliveryAddress,
                                            OrderStatus = x.OrderStatus.ToString(),
                                            OrderNumber = x.OrderTrackingId,
                                            IsConfirmed = x.IsConfirmed,
                                            OrderLineItems = x.OrderLineItems.Select(oli => new OrderLineItemDetailsDto()
                                            {
                                                Id = oli.Id,
                                                IsActive = oli.IsActive,
                                                ProductName = oli.OrderedProduct.NormalizedName,
                                                OrderCount = oli.QuantityOrdered
                                            }).ToList()
                                        })
                                        .SingleOrDefaultAsync();

            if(orderDetails is null)
            {
                Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Information("No Order exists for Id - {Id}", OrderId);
                return GenericResponse<OrderDetailsDto>.Failure(null, $"No Order exists for Id: {OrderId}", System.Net.HttpStatusCode.NotFound);
            }

            Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Information("Order Details Fetched Successfully for Order - {Id}. Details: {orderDetails}", OrderId, JsonSerializer.Serialize(orderDetails));

            return GenericResponse<OrderDetailsDto>.Success(orderDetails, "Order Details Fetched Successfully.", System.Net.HttpStatusCode.OK);
        }
        catch(DbException ex)
        {
            Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Error(ex, "A database error occurred fetching order details for order - {Id}", OrderId);
            return GenericResponse<OrderDetailsDto>.Failure(null, "A database error occurred fetching order details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Error(ex, "An error occurred fetching order details for order - {Id}", OrderId);
            return GenericResponse<OrderDetailsDto>.Failure(null, "An error occurred fetching order details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }


    private string GetOrderTrackingId()
    {
        string dateToString = DateTime.Now.ToString("yyyyddMMHHmmssfff");
        return $"O-{dateToString}-{Random.Shared.Next(1000, 9999)}";
    }

    private string GetOrderVerificationToken()
    {
        byte[] randBytes = new byte[64];

        using(var randGen = RandomNumberGenerator.Create())
        {
            randGen.GetBytes(randBytes);
        }

        return Convert.ToHexString(randBytes);
    }
}
