using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Controllers.AuthRequirements;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Entities.StaticValues;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.MailOperation;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;
using ProductManagementSystem.Shared.DataTransferObjects.RequestParameters;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Data.Common;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace ProductManagementSystem.Api.Services;

public class OrderService : IOrderService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly IEmailVerificationLinkFactory _emailVerificationLinkFactory;
    private readonly IEmailService _emailService;
    private readonly UserOrderVerificationConfig _userOtpVerificationConfig;
    private readonly IRedisService _redisService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly StackExchange.Redis.IDatabase _redisDatabase;

    private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

    public OrderService(RepositoryContext repositoryContext, IEmailVerificationLinkFactory emailVerificationLinkFactory,
                        IEmailService emailService, IOptionsMonitor<UserOrderVerificationConfig> optionsMonitor,
                        IRedisService redisService, IAuthorizationService authorizationService, 
                        IHttpContextAccessor httpContextAccessor, StackExchange.Redis.IConnectionMultiplexer connectionMultiplexer)
    {
        _repositoryContext = repositoryContext;
        _emailVerificationLinkFactory = emailVerificationLinkFactory;
        _emailService = emailService;
        _userOtpVerificationConfig = optionsMonitor.CurrentValue;
        _redisService = redisService;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _redisDatabase = connectionMultiplexer.GetDatabase();
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
            Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Create Order - {orderToCreate}", createOrder);

            //Validate user Exists
            User? createdByUser = await _repositoryContext.Users.FirstOrDefaultAsync(x => x.Id == createOrder.UserId);

            if(createdByUser is null)
            {
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("User with Id does not exist - {0}", createOrder.UserId);
                return GenericResponse<OrderDto>.Failure(null, "User with Id does not exist", System.Net.HttpStatusCode.NotFound);
            }

            //Validate the product to order exists and the quantity ordered is available in stock before creating the order

            List<int> productIds = createOrder.OrderLineItems.Select(x => x.ProductId).ToList();
            //Begin to obtain lock on the product ids
            int retryCount = 0; bool isLockObtained = false;

            while(!isLockObtained && retryCount < 3)
            {
                isLockObtained = await TryAcquireLockOnProducts(productIds);
                await Task.Delay(100);
            }

            if(retryCount >= 3 && !isLockObtained)
            {
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Product Id lock could not be obtained after {0} retrued.", retryCount);
                return GenericResponse<OrderDto>.Failure(null, "Operation could not be performed. Kindly retry.", System.Net.HttpStatusCode.Conflict);
            }

            List<Product> productsToOrder = await _repositoryContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

            if(productIds.Count != productsToOrder.Count)
            {
                var existingProductIds = productsToOrder.Select(x => x.Id);
                var nonExistingProductIds = productIds.Except(existingProductIds);

                var removeLockedProduct = await RemoveProductLockFromCache(productIds);
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("The following product Ids do not exist: {nonExistingProductIds}. Remove locked products returns - {removeLocked}", JsonSerializer.Serialize(nonExistingProductIds), removeLockedProduct);
                return GenericResponse<OrderDto>.Failure(null, $"The following product Ids do not exist: {JsonSerializer.Serialize(nonExistingProductIds)}", System.Net.HttpStatusCode.NotFound);
            }

            var joinedOrderItems = createOrder.OrderLineItems.Join(productsToOrder, oli => oli.ProductId, p => p.Id, (oli, p) => new { OrderLineItem = oli, Product = p, Rate = oli.QuantityOrdered * p.SellingPrice, StockAvailable = p.CurrentCount >= oli.QuantityOrdered }).ToList();

            if(joinedOrderItems.Any(x => !x.StockAvailable))
            {
                var removeLockedProduct = await RemoveProductLockFromCache(productIds);
                var outOfStockProducts = joinedOrderItems.Where(x => !x.StockAvailable).Select(x => new { x.Product.Id, x.Product.NormalizedName, x.Product.CurrentCount, x.OrderLineItem.QuantityOrdered });
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("The following products are out of stock: {outOfStockProducts}. Remove locked products returns - {response}", JsonSerializer.Serialize(outOfStockProducts), removeLockedProduct);
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
                CreatedBy = $"{createdByUser.FirstName} {createdByUser.LastName}",
                OrderStatus = Entities.StaticValues.OrderStatus.Pending,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                DeliveryAddress = createOrder.DeliveryAddress,
                OrderTrackingId = GetOrderTrackingId(),
                UserId = createdByUser.Id
            };

            //Create order line items from the order line items from request and set to the order line items collection of the order to insert

            List<OrderLineItem> orderLineItems = joinedOrderItems.Select(x => new OrderLineItem()
            {

                ProductId = x.Product.Id,
                QuantityOrdered = x.OrderLineItem.QuantityOrdered,
                OrderRate = x.Rate
               

            }).ToList();

            orderToInsert.OrderLineItems = orderLineItems;
            orderToInsert.UserOrderVerificationTokens.Add(new UserOrderVerificationToken()
            {
                VerificationToken = GetOrderVerificationToken()
            });

            //Add the order to the database and update the product count for the ordered product
            await _repositoryContext.Orders.AddAsync(orderToInsert);
            //_repositoryContext.Products.Update(productToOrder);

            try
            {
                await _repositoryContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Error(ex, "An error ocurred updating database.");
                return GenericResponse<OrderDto>.Failure(null, "An error occurred inserting record into database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
            }
            finally
            {
                var removeLockedProduct = await RemoveProductLockFromCache(productIds);
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Remove locked out products returns - {0}", removeLockedProduct);
            }


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
                CreatedDate = orderToInsert.CreatedAt.ToLocalTime(),
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
            Order? order = await _repositoryContext.Orders.IgnoreQueryFilters().Include(x => x.OrderLineItems).SingleOrDefaultAsync(x => x.Id == Id);

            if(order is null)
            {
                Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Information("Order does not exist - {Id}", Id);
                return GenericResponse<string>.Failure("Operation Failed.", $"Order does not exist for Id: {Id}.", System.Net.HttpStatusCode.NotFound);
            }

            var isValid = await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, order, Operations.Delete);

            if (!isValid.Succeeded)
            {
                Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Information("User - {0} is not authenticated to perform action on accesseed resource. Allowed User - {1}", 
                                                                                                    _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), order.UserId);

                return GenericResponse<string>.Failure("Operation Failed.", "Access denied.", System.Net.HttpStatusCode.Forbidden);
            }

            List<int> productIds = order.OrderLineItems.Select(x => x.ProductId).ToList();
            //Begin to obtain lock on the product ids
            int retryCount = 0; bool isLockObtained = false;

            while (!isLockObtained && retryCount < 3)
            {
                isLockObtained = await TryAcquireLockOnProducts(productIds);
                await Task.Delay(100);
            }

            if (retryCount >= 3 && !isLockObtained)
            {
                Log.ForContext(_methodName, "CreateAsync").ForContext(_className, "OrderService").Information("Product Id lock could not be obtained after {0} retrued.", retryCount);
                return GenericResponse<string>.Failure("Operation could not be completed.", "Operation could not be performed. Kindly retry.", System.Net.HttpStatusCode.Conflict);
            }

            List<Product> productsToOrder = await _repositoryContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

            //Loop through the order line items and increase the count of each product in the order line item by the quantity ordered for that product in the order line item

            var orderItems = order.OrderLineItems.Where(x => x.IsActive).ToList();

            var joinedProducts = orderItems.Join(productsToOrder, x => x.ProductId, y => y.Id, (oli, p) => new { p, oli });

            //foreach (var orderItem in order.OrderLineItems.Where(x => x.IsActive).ToList())
            //{
            //    int quantityOrdered = orderItem.QuantityOrdered;
            //    orderItem.OrderedProduct.CurrentCount += quantityOrdered;
            //}

            foreach (var item in joinedProducts)
            {
                int orderedQuantity = item.oli.QuantityOrdered;
                item.p.CurrentCount += orderedQuantity;
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

            try
            {
                await _repositoryContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Error(ex, "Database error occurred performing operation");
                return GenericResponse<string>.Failure("Operation Failed.", "Database error occurred.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
            }
            finally
            {
                var result = await RemoveProductLockFromCache(productIds);
                Log.ForContext(_methodName, "DeleteAsync").ForContext(_className, "OrderService").Information("Remove from lockout returns - {0}", result);
            }

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

            var ordersFromCache = await _redisService.GetItemAsync<List<OrderDto>>(RedisCacheHelperClass.OrdersKey);

            if(ordersFromCache is not null && ordersFromCache.Any())
            {
                Log.ForContext(_methodName, "GetAllAsync").ForContext(_className, "OrderService").Information("Orders Fetched from cache - {0}", ordersFromCache);
                return GenericResponse<IEnumerable<OrderDto>>.Success(ordersFromCache, "Order Fetched Successfully.", System.Net.HttpStatusCode.OK);
            }

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

            var setItemToCache = await _redisService.SetItemAsync<List<OrderDto>>(orders, RedisCacheHelperClass.OrdersKey, 18400);

            Log.ForContext(_methodName, "GetAllAsync").ForContext(_className, "OrderService").Information("Orders Fetched Successfully - {orders}, Set Item to cache - {setToCache}", JsonSerializer.Serialize(orders), setItemToCache);

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

    public async Task<GenericResponse<PaginatedList<OrderDto>>> GetAllOrdersAsync(OrderRequestParameters orderRequestParameters)
    {
        try
        {
            Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(GetAllOrdersAsync)).Information("Fetching Orders with parameters - {0}", orderRequestParameters);

            var startDatetime = orderRequestParameters.StartDate.ToDateTime(new TimeOnly(0, 0, 0, 0));
            var endDatetime = orderRequestParameters.EndDate.ToDateTime(new TimeOnly(23, 59, 59, 999));

            var queryableItem = _repositoryContext.Orders.Where(x => x.CreatedAt >= startDatetime && x.CreatedAt <= endDatetime)
                                                .Skip(orderRequestParameters.PageNumber - 1)
                                                .Take(orderRequestParameters.PageSize);

            if (!string.IsNullOrEmpty(orderRequestParameters.OrderStatus))
            {
                if(Enum.TryParse<Entities.StaticValues.OrderStatus>(orderRequestParameters.OrderStatus, ignoreCase: true, out var enumResult))
                {
                    queryableItem = queryableItem.Where(x => x.OrderStatus == enumResult);
                }
            }

            if (!string.IsNullOrEmpty(orderRequestParameters.TrackingNumber))
            {
                queryableItem = queryableItem.Where(x => EF.Functions.Like(x.OrderTrackingId, orderRequestParameters.TrackingNumber));
            }

            if (orderRequestParameters.ProductId.HasValue)
            {
                queryableItem = queryableItem.Where(x => x.OrderLineItems.Any(x => x.ProductId == orderRequestParameters.ProductId.Value));
            }

            int totalCount = await queryableItem.CountAsync();

            List<OrderDto> items = await queryableItem.Select(x => new OrderDto()
                                                {
                                                    Id = x.Id,
                                                    OrderStatus = x.OrderStatus.ToString(),
                                                    CreatedDate = x.CreatedAt.ToLocalTime(),
                                                    CreatedBy = x.CreatedBy,
                                                    LineItemsCount = x.OrderLineItems.Count,
                                                    OrderNumber = x.OrderTrackingId
                                                }).ToListAsync();

            Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(GetAllOrdersAsync)).Information("Result returns total Count: {0} - {1}", totalCount, items);

            var pagedItem = PaginatedList<OrderDto>.ToPagedList(items, orderRequestParameters.PageSize, orderRequestParameters.PageNumber, totalCount);

            return GenericResponse<PaginatedList<OrderDto>>.Success(pagedItem, "Orders Fetced Successfully.", System.Net.HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(GetAllOrdersAsync)).Error(ex, "An error occurred fetching paginated ordrs.");
            return GenericResponse<PaginatedList<OrderDto>>.Failure(null, "An error occurred fetching orders.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        throw new NotImplementedException();
    }

    public async Task<GenericResponse<OrderDto>> GetByIdAsync(int Id)
    {
        try
        {
            Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Information("Fetch Products by Id - {Id}", Id);

            var orderFromCcahe = await _redisService.GetItemAsync<OrderDto>(RedisCacheHelperClass.GetOrderCacheKey(Id));

            if(orderFromCcahe is not null)
            {
                Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Information("Order retrived from cacahe successfully - {0}", orderFromCcahe);
                return GenericResponse<OrderDto>.Success(orderFromCcahe, "Order Fetched Successfully.", System.Net.HttpStatusCode.OK);
            }

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

            var setItemToCache = await _redisService.SetItemAsync<OrderDto>(order, RedisCacheHelperClass.GetOrderCacheKey(Id), 18400);

            Log.ForContext(_methodName, "GetByIdAsync").ForContext(_className, "OrderService").Information("Order with Id: {Id} Fetched Successully - {order}. Set Item to cache - {setCache}", Id, JsonSerializer.Serialize(order), setItemToCache);
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

            var isValidRequirement = await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, orderToUpdate, Operations.Update);

            if (!isValidRequirement.Succeeded)
            {
                Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Warning("User - {0} is not auhtorized to perform operation on the resource. Allowed User - {1}", 
                                                                    _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), orderToUpdate.UserId);
                return GenericResponse<OrderDto>.Failure(null, "Operation denied.", System.Net.HttpStatusCode.Forbidden);
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
                CreatedDate = orderToUpdate.CreatedAt,
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

            var isValidRequirement = await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, orderToUpdate, Operations.Update);

            if (!isValidRequirement.Succeeded)
            {
                Log.ForContext(_methodName, "UpdateAsync").ForContext(_className, "OrderService").Warning("User - {0} is not auhtorized to perform operation on the resource. Allowed User - {1}",
                                                                    _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), orderToUpdate.UserId);
                return GenericResponse<string>.Failure("Operation Failed.", "Operation denied.", System.Net.HttpStatusCode.Forbidden);
            }

            if (orderToUpdate.OrderStatus == Entities.StaticValues.OrderStatus.Delivered || orderToUpdate.OrderStatus == Entities.StaticValues.OrderStatus.Cancelled)
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

            var orderDetailFromCache = await _redisService.GetItemAsync<OrderDetailsDto>(RedisCacheHelperClass.GetOrderDetailKey(OrderId));

            if(orderDetailFromCache is not null)
            {
                Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Information("Order Detail retrieved from cache successfully - {0}", orderDetailFromCache);
                return GenericResponse<OrderDetailsDto>.Success(orderDetailFromCache, "Order Details Fetched Successfully.", System.Net.HttpStatusCode.OK);
            }

            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0";
            bool isAdmin = _httpContextAccessor.HttpContext.User.IsInRole("ADMIN") || _httpContextAccessor.HttpContext.User.IsInRole("SYSTEM");

            var operationQuery = _repositoryContext.Orders.AsNoTracking().Where(x => x.Id == OrderId);

            if (!isAdmin)
            {
                operationQuery = operationQuery.Where(x => x.UserId == int.Parse(userId));
            }

            OrderDetailsDto? orderDetails = await operationQuery
                                        .Select(x => new OrderDetailsDto()
                                        {
                                            Id = x.Id,
                                            CreatedBy = x.CreatedBy,
                                            CreatedDate = x.CreatedAt.ToLocalTime(),
                                            DeliveryAddress = x.DeliveryAddress,
                                            OrderStatus = x.OrderStatus.ToString(),
                                            OrderNumber = x.OrderTrackingId,
                                            IsConfirmed = x.IsConfirmed,
                                            CreatedByUser = string.Concat(x.CreatedByUser.FirstName, " ", x.CreatedByUser.LastName),
                                            OrderLineItems = x.OrderLineItems.Select(oli => new OrderLineItemDetailsDto()
                                            {
                                                Id = oli.Id,
                                                IsActive = oli.IsActive,
                                                ProductName = oli.OrderedProduct.NormalizedName,
                                                OrderCount = oli.QuantityOrdered,
                                                Price = oli.OrderRate * oli.QuantityOrdered
                                            }).ToList()
                                        })
                                        .SingleOrDefaultAsync();

            if(orderDetails is null)
            {
                Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Information("No Order exists for Id - {Id}", OrderId);
                return GenericResponse<OrderDetailsDto>.Failure(null, $"No Order exists for Id: {OrderId}", System.Net.HttpStatusCode.NotFound);
            }

            var setItemToCache = await _redisService.SetItemAsync<OrderDetailsDto>(orderDetails, RedisCacheHelperClass.GetOrderDetailKey(OrderId), 18400);

            Log.ForContext(_methodName, "GetOrderDetailsAsync").ForContext(_className, "OrderService").Information("Order Details Fetched Successfully for Order - {Id}. Details: {orderDetails}. Set Item to cache - {setCache}", OrderId, JsonSerializer.Serialize(orderDetails), setItemToCache);

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

    public async Task<GenericResponse<IEnumerable<OrderDto>>> GetUserOrdersAsync(int UserId)
    {

        try
        {
            Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(GetUserOrdersAsync)).Information("Get User Orders - {0}", UserId);

            List<OrderDto> userOrders = await _repositoryContext.Orders.Where(x => x.UserId == UserId).Select(x => new OrderDto()
            {
                Id = x.Id,
                OrderStatus = x.OrderStatus.ToString(),
                CreatedBy = x.CreatedBy,
                CreatedDate = x.CreatedAt,
                LineItemsCount = x.OrderLineItems.Count,
                OrderNumber = x.OrderTrackingId
            }).ToListAsync();

            Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(GetUserOrdersAsync)).Information("Fetched User Orders - {0}", userOrders);

            return userOrders.Any() ?
                GenericResponse<IEnumerable<OrderDto>>.Success(userOrders, "User Orders Fetched Successfully.", System.Net.HttpStatusCode.OK) :
                GenericResponse<IEnumerable<OrderDto>>.Failure(null, "No Order Fetched with user.", System.Net.HttpStatusCode.NotFound);

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(GetUserOrdersAsync)).Error(ex, "An Error Occurred Fetching User Orders.");
            return GenericResponse<IEnumerable<OrderDto>>.Failure(null, "An Error Occurred Fetching User Orders.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
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

        using (var randGen = RandomNumberGenerator.Create())
        {
            randGen.GetBytes(randBytes);
        }

        return Convert.ToHexString(randBytes);
    }

    private async Task<(bool isSuccessful, long removeCount)> RemoveProductLockFromCache(List<int> productIds)
    {
        StackExchange.Redis.RedisKey[] productKeys = productIds.Select(x => new StackExchange.Redis.RedisKey(RedisCacheHelperClass.GetLockedOutProductCacheKey(x))).ToArray();
        var result = await _redisDatabase.KeyDeleteAsync(productKeys);

        return (productIds.Count == result, result);
    }

    private async Task<bool> TryAcquireLockOnProducts(List<int> productIds)
    {
        //Begin locking and obtaining lock operation
        Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(TryAcquireLockOnProducts)).Information("Getting locked status for - {0}", productIds);

        //Check if semaphore lock is available within a 500ms timeout
        bool isAavailable = await _semaphoreSlim.WaitAsync(500);

        //Semaphore not available after timeout - return false to consumer
        if (!isAavailable)
        {
            return false;
        }


        //Semaphore available - Begin main operation
        try
        {
            //Get all the keys from the list of products
            //var keys = productIds.Select(x => RedisCacheHelperClass.GetLockedOutProductCacheKey(x)).ToArray();

            //StackExchange.Redis.RedisKey[] redisKeys = keys.Select(x => new StackExchange.Redis.RedisKey(x)).ToArray();

            //Obtain any possible lock keys
            //var availableKeysCount = await _redisDatabase.KeyExistsAsync(redisKeys);

            //One or more keys in the product is currently locked
            //if(availableKeysCount > 0)
            //{
            //    return false;
            //}

            //No product key is currently locked - Begin set such keys

            KeyValuePair<StackExchange.Redis.RedisKey, StackExchange.Redis.RedisValue>[] keysToAdd = productIds.Select(x => new KeyValuePair<StackExchange.Redis.RedisKey, StackExchange.Redis.RedisValue>(new StackExchange.Redis.RedisKey(RedisCacheHelperClass.GetLockedOutProductCacheKey(x)), x)).ToArray();

            //Set Multiple and set timeout to be after 10seconds
            var setKeys = await _redisDatabase.StringSetAsync(keysToAdd, when: StackExchange.Redis.When.NotExists, expiry: TimeSpan.FromSeconds(10));

            return setKeys;
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(OrderService)).ForContext(_methodName, nameof(TryAcquireLockOnProducts)).Error(ex, "An error occurred while obtaining lock on products.");
            return false;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}
