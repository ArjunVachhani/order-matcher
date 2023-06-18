namespace OrderMatcher.Types;

public enum SelfMatchAction : byte
{
    Match = 0,
    CancelNewest = 1,
    CancelOldest = 2,
    Decrement = 3,
}
