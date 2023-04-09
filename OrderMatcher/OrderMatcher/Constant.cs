using System.Diagnostics.CodeAnalysis;

namespace OrderMatcher;

[SuppressMessage("Microsoft.Naming", "CA1707")]
public static class Constant
{
    public const string ORDER_QUANTITY_IS_LESS_THEN_REQUESTED_FILL_QUANTITY = "Order quantity is less then requested fill quanity";
    public const string NOT_EXPECTED = "Not expected";
}
