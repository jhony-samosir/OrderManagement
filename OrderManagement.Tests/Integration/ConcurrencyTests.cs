using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Model.DTOs.Requests;
using OrderManagement.Model.Entities;
using OrderManagement.Model.Enums;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrderManagement.Tests.Integration;

[Collection("OrderApi")]
public sealed class ConcurrencyTests(OrderApiFixture factory) : IClassFixture<OrderApiFixture>
{
    private readonly HttpClient _client = factory.CreateClient();
    [Fact]
    public async Task CreateOrder_ConcurrentRequests_SameIdempotencyKey_OnlyOneOrderCreated()
    {
        var product = await SeedProductWithStock(stock: 100);
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = BuildCreateRequest(product.Id);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => PostOrder(request, idempotencyKey))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        var successCodes = new[] { HttpStatusCode.Created, HttpStatusCode.OK };
        var successCount = responses.Count(r => successCodes.Contains(r.StatusCode));
        successCount.Should().BeGreaterThanOrEqualTo(1, "at least one request must succeed");

        using var db = factory.CreateDbContext();
        var orderCount = await db.Orders
            .Where(o => o.IdempotencyKey == idempotencyKey)
            .CountAsync();
        orderCount.Should().Be(1, "exactly one order should exist for a given idempotency key");
    }

    [Fact]
    public async Task CreateOrder_ConcurrentRequests_InsufficientStock_StockNeverGoesNegative()
    {
        const int initialStock = 15;
        const int quantityPerOrder = 2;
        const int concurrentRequests = 10;
        const int maxSuccessful = initialStock / quantityPerOrder; 

        var product = await SeedProductWithStock(stock: initialStock);

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => PostOrder(BuildCreateRequest(product.Id, quantityPerOrder), Guid.NewGuid().ToString()))
            .ToList();

        var responses = await Task.WhenAll(tasks);
                
        using var db = factory.CreateDbContext();
        var updatedProduct = await db.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().BeGreaterThanOrEqualTo(0,
            "stock must never become negative — atomic UPDATE WHERE ensures this");

        var successfulOrders = responses.Count(r => r.IsSuccessStatusCode);
        successfulOrders.Should().BeLessThanOrEqualTo(maxSuccessful,
            $"cannot fulfill more than {maxSuccessful} orders of {quantityPerOrder} units each from {initialStock} units");

        var failedOrders = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        failedOrders.Should().BeGreaterThanOrEqualTo(concurrentRequests - maxSuccessful,
            "requests exceeding stock capacity should return 409 Conflict");
    }

    [Fact]
    public async Task UpdateStatus_ConcurrentRequests_OnlyOneSucceeds_OthersGet409()
    {
        var product = await SeedProductWithStock(stock: 50);
        var createResponse = await PostOrder(BuildCreateRequest(product.Id), Guid.NewGuid().ToString());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var orderId = ((System.Text.Json.JsonElement)created!).GetProperty("id").GetString();
                
        await _client.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });
                
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _client.PatchAsJsonAsync(
                $"/api/orders/{orderId}/status",
                new UpdateOrderStatusRequest { Status = OrderStatus.Shipped }))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        var okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        okCount.Should().Be(1, "only one concurrent update should succeed");
        conflictCount.Should().Be(4, "other 4 should get 409 Conflict from optimistic locking");
    }

    [Fact]
    public async Task CancelOrder_ConcurrentCancels_StockRestoredExactlyOnce()
    {
        const int quantity = 5;
        const int initialStock = 20;

        var product = await SeedProductWithStock(stock: initialStock);
        var createResponse = await PostOrder(BuildCreateRequest(product.Id, quantity), Guid.NewGuid().ToString());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var orderId = ((System.Text.Json.JsonElement)created!).GetProperty("id").GetString();
                
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _client.PostAsync($"/api/orders/{orderId}/cancel", null))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        var okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        okCount.Should().Be(1, "only one cancel should succeed");
                
        using var db = factory.CreateDbContext();
        var updatedProduct = await db.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(initialStock, "stock must be restored exactly once — xmin optimistic lock prevents double-restore");
    }    

    private async Task<Product> SeedProductWithStock(int stock)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = $"Test Product {Guid.NewGuid():N}",
            Price = 10m,
            StockQuantity = stock
        };
        await factory.SeedProductAsync(product);
        return product;
    }
    private static CreateOrderRequest BuildCreateRequest(Guid productId, int quantity = 1) => new()
    {
        CustomerId = Guid.NewGuid(),
        ShippingAddress = "123 Test Street, Jakarta",
        Items = [new OrderItemRequest { ProductId = productId, Quantity = quantity }]
    };
    private Task<HttpResponseMessage> PostOrder(CreateOrderRequest request, string idempotencyKey)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        return _client.SendAsync(message);
    }
}
