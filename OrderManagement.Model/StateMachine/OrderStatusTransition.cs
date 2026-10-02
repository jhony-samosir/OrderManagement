using OrderManagement.Model.Enums;

namespace OrderManagement.Model.StateMachine;

public static class OrderStatusTransition
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],   
        [OrderStatus.Cancelled] = [],   
    };
    
    public static bool IsValid(OrderStatus from, OrderStatus to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    
    public static bool IsTerminal(OrderStatus status)
        => ValidTransitions.TryGetValue(status, out var next) && next.Count == 0;
    
    public static IReadOnlySet<OrderStatus> GetAllowed(OrderStatus from)
        => ValidTransitions.TryGetValue(from, out var allowed)
            ? allowed
            : (IReadOnlySet<OrderStatus>)new HashSet<OrderStatus>();
}
