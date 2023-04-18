namespace OrderMatcher.Types;

public class OrderMatcherException : Exception
{
    public OrderMatcherException() { }

    public OrderMatcherException(string message) : base(message) { }

    public OrderMatcherException(string message, Exception innerException) : base(message, innerException) { }
}
