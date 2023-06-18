﻿namespace OrderMatcher.Types;

public enum CancelReason : byte
{
    UserRequested = 1,
    MarketOrderNoLiquidity = 2,
    ImmediateOrCancel = 3,
    FillOrKill = 4,
    BookOrCancel = 5,
    ValidityExpired = 6,
    MarketOrderCannotMatchLessThanStepSize = 7,
    InvalidOrder = 8,
    SelfMatch = 9,
}
