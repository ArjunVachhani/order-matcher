namespace OrderMatcher.Types;

public enum OrderCondition : byte
{
    None = 0,
    ImmediateOrCancel = 1,
    BookOrCancel = 2,
    FillOrKill = 4,
}
// 0000 0000  None
// 0000 0001  ImmediateOrCancel
// 0000 0010  BookOrCancel
// 0000 1000  FillOrKill