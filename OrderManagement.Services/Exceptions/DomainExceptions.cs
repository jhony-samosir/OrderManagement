namespace OrderManagement.Service.Exceptions;


public abstract class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string resourceName, object key)
    : DomainException($"{resourceName} with key '{key}' was not found.");

public sealed class InsufficientStockException(string productName, int available, int requested)
    : DomainException(
        $"Product '{productName}' only has {available} unit(s) available, but {requested} were requested.");

public sealed class ConflictException(string message) : DomainException(message);

public sealed class InvalidStatusTransitionException(string from, string to, IEnumerable<string> allowed)
    : DomainException(
        $"Cannot transition order from '{from}' to '{to}'. " +
        $"Allowed transitions: [{string.Join(", ", allowed)}].");
