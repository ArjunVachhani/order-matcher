using OrderMatcher.Types;
using System.Collections;
using System.Collections.Generic;

namespace OrderMatcher
{
    internal class OrderIdTracker
    {
        private readonly SortedSet<RangeTracker> _ranges = new SortedSet<RangeTracker>();
        private readonly RangeTracker _rangeForSearch = new RangeTracker(0, 1);
        private readonly int _rangeTrackerSize;

        private RangeTracker? _cached;
        private int _markAfterCompaction;

        public OrderIdTracker(int rangeTrackerSize)
        {
            _rangeTrackerSize = rangeTrackerSize;
        }

        public bool TryMark(OrderId orderId)
        {
            var range = GetOrCreateRange(orderId);
            var result = range.TryMark(orderId);

            if (++_markAfterCompaction >= _rangeTrackerSize * 4)
            {
                Compact();
                _markAfterCompaction = 0;
            }

            return result;
        }

        private RangeTracker GetOrCreateRange(OrderId orderId)
        {
            if (_cached != null && orderId >= _cached.FromOrderId && orderId <= _cached.ToOrderId)
                return _cached;

            _rangeForSearch.ExtendMarkFromOrderId(orderId);
            _rangeForSearch.ExtendMarkToOrderId(orderId);
            if (_ranges.TryGetValue(_rangeForSearch, out _cached))
                return _cached;

            var startOrderId = orderId - (orderId % _rangeTrackerSize);
            var newRange = new RangeTracker(startOrderId, _rangeTrackerSize);
            _ranges.Add(newRange);
            _cached = newRange;
            return _cached;
        }

        public void Compact()
        {
            RangeTracker? previousRange = null;
            foreach (var range in _ranges)
            {
                if (previousRange == null)
                    previousRange = range;

                if (range.CountUnmarkedInBitArray() == 0 && previousRange.ToOrderId + 1 == range.FromOrderId)
                {
                    previousRange.ExtendMarkToOrderId(range.ToOrderId);
                }
            }
        }
    }

    internal class RangeTracker
    {
        private readonly OrderId _bitArrayStartOrderId;
        private BitArray? _bitArray;

        public OrderId FromOrderId { get; private set; }
        public OrderId ToOrderId { get; private set; }

        public RangeTracker(OrderId fromOrderId, int length)
        {
            FromOrderId = fromOrderId;
            _bitArrayStartOrderId = fromOrderId;
            ToOrderId = fromOrderId + length;
            if (length > 1)
                _bitArray = new BitArray(length);
        }

        private bool AllowedInBitArrayRange(OrderId orderId) => _bitArray != null && orderId >= _bitArrayStartOrderId && orderId <= (_bitArrayStartOrderId + _bitArray.Length);

        public bool TryMark(OrderId orderId)
        {
            if (AllowedInBitArrayRange(orderId))
            {
                int index = (int)(orderId - _bitArrayStartOrderId);
                if (!_bitArray!.Get(index))
                {
                    _bitArray!.Set(index, true);
                    return true;
                }
            }
            return false;
        }

        public int CountUnmarkedInBitArray()
        {
            if (_bitArray == null)
                return 0;

            int count = 0;
            for (int i = 0; i < _bitArray.Length; i++)
            {
                if (!_bitArray[i])
                    count++;
            }

            if (count == 0)
                _bitArray = null;

            return count;
        }

        public bool IsMarked(OrderId orderId)
        {
            if (_bitArray == null || AllowedInBitArrayRange(orderId))
            {
                if (_bitArray == null)
                    return true;

                int index = (int)(orderId - _bitArrayStartOrderId);
                return _bitArray[index];
            }
            return false;
        }

        public void ExtendMarkFromOrderId(OrderId fromOrderId)
        {
            FromOrderId = fromOrderId;
        }

        public void ExtendMarkToOrderId(OrderId toOrderId)
        {
            ToOrderId = toOrderId;
        }
    }

    internal class RangeTrackerComparer : IComparer<RangeTracker>
    {
        public int Compare(RangeTracker x, RangeTracker y)
        {
            if (x.FromOrderId < y.FromOrderId)
                return -1;
            else if (x.ToOrderId > y.ToOrderId)
                return 1;
            else if (x.FromOrderId >= y.FromOrderId && x.ToOrderId <= y.ToOrderId)
                return 0;
            else
                throw new OrderMatcherException("RangeTrackerComparer :: Not expected");
        }
    }
}