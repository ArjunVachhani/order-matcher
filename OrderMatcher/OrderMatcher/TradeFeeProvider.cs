using System.Collections.Generic;

namespace OrderMatcher
{
    public class TradeFeeProvider : ITradeFeeProvider
    {
        private readonly SortedList<short, TradeFee> _tradeFee;
        private TradeFee _cachedFee;
        public TradeFeeProvider()
        {
            _tradeFee = new SortedList<short, TradeFee>();
        }
        public TradeFee GetTradeFee(short tradeFeeId)
        {
            if (_cachedFee?.TradeFeeId == tradeFeeId)
                return _cachedFee;
            else
            {
                if (_tradeFee.TryGetValue(tradeFeeId, out var fee))
                {
                    return fee;
                }
                else
                {
                    fee = FetchFromDb(tradeFeeId);
                    _tradeFee.Add(tradeFeeId, fee);
                    if (_cachedFee == null)
                        _cachedFee = fee;

                    return fee;
                }
            }
        }

        private TradeFee FetchFromDb(short tradeFeeId)
        {
            //TODO fetch from db
            return new TradeFee { TradeFeeId = tradeFeeId, FeeCurreny = FeeCurreny.AlwaysQuoteCurrency, MakerFee = 0.1m, TakerFee = 0.2m };
        }
    }
}
