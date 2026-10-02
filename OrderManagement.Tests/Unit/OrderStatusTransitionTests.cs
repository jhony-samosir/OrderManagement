using FluentAssertions;
using OrderManagement.Model.Enums;
using OrderManagement.Model.StateMachine;
using Xunit;

namespace OrderManagement.Tests.Unit;





public sealed class OrderStatusTransitionTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]  
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]  
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled, false)]  
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]  
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending, false)]  
    public void IsValid_ShouldReturnExpectedResult(OrderStatus from, OrderStatus to, bool expected)
    {
        var result = OrderStatusTransition.IsValid(from, to);
        result.Should().Be(expected, $"transition {from} → {to} should be {(expected ? "valid" : "invalid")}");
    }

    [Theory]
    [InlineData(OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.Shipped, false)]
    public void IsTerminal_ShouldCorrectlyIdentifyTerminalStates(OrderStatus status, bool isTerminal)
    {
        OrderStatusTransition.IsTerminal(status).Should().Be(isTerminal);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, new[] { OrderStatus.Confirmed, OrderStatus.Cancelled })]
    [InlineData(OrderStatus.Confirmed, new[] { OrderStatus.Shipped, OrderStatus.Cancelled })]
    [InlineData(OrderStatus.Shipped, new[] { OrderStatus.Delivered })]
    [InlineData(OrderStatus.Delivered, new OrderStatus[] { })]
    [InlineData(OrderStatus.Cancelled, new OrderStatus[] { })]
    public void GetAllowed_ShouldReturnCorrectTransitions(OrderStatus from, OrderStatus[] expected)
    {
        var allowed = OrderStatusTransition.GetAllowed(from);
        allowed.Should().BeEquivalentTo(expected);
    }
}
