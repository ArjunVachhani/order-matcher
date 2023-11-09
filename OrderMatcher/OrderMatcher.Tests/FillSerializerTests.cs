﻿using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class FillSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Quantity.MinValue, BidFee = Quantity.MinValue, AskRemainingQuantity = Quantity.MinValue, AskFee = Quantity.MinValue, MessageSequence = long.MinValue }, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Quantity.MaxValue, BidFee = Quantity.MaxValue, AskRemainingQuantity = Quantity.MaxValue, AskFee = Quantity.MaxValue, MessageSequence = long.MaxValue }, bytes);
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FillSerializer.Serialize(null, null));
            Assert.Equal("fill", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FillSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[134];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 135", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[136];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 135", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[135];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[135];
            bytes[4] = (byte)MessageType.Fill;
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Quantity.MinValue, BidFee = Quantity.MinValue, AskRemainingQuantity = Quantity.MinValue, AskFee = Quantity.MinValue, MessageSequence = long.MinValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(135, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MinValue, fill.TakerOrderId);
            Assert.Equal(int.MinValue, fill.MatchRate);
            Assert.Equal(int.MinValue, fill.MatchQuantity);
            Assert.Equal(int.MinValue, fill.Timestamp);
            Assert.Equal(Quantity.MinValue, fill.BidCost);
            Assert.Equal(Quantity.MinValue, fill.BidFee);
            Assert.Equal(Quantity.MinValue, fill.AskRemainingQuantity);
            Assert.Equal(Quantity.MinValue, fill.AskFee);
            Assert.Equal(long.MinValue, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Quantity.MaxValue, BidFee = Quantity.MaxValue, AskRemainingQuantity = Quantity.MaxValue, AskFee = Quantity.MaxValue, MessageSequence = long.MaxValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(135, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MaxValue, fill.TakerOrderId);
            Assert.Equal(int.MaxValue, fill.MatchRate);
            Assert.Equal(int.MaxValue, fill.MatchQuantity);
            Assert.Equal(int.MaxValue, fill.Timestamp);
            Assert.Equal(Quantity.MaxValue, fill.AskRemainingQuantity);
            Assert.Equal(Quantity.MaxValue, fill.BidCost);
            Assert.Equal(Quantity.MaxValue, fill.AskFee);
            Assert.Equal(Quantity.MaxValue, fill.BidFee);
            Assert.Equal(long.MaxValue, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, BidFee = 76157, AskRemainingQuantity = 87135, AskFee = 12103, MessageSequence = 6812379 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(135, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Equal(76157, fill.BidFee);
            Assert.Equal(87135, fill.AskRemainingQuantity);
            Assert.Equal(12103, fill.AskFee);
            Assert.Equal(6812379, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_BidCostNull()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = null, BidFee = null, AskRemainingQuantity = 87135, AskFee = 5434, MessageSequence = 123879 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(135, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Null(fill.BidCost);
            Assert.Null(fill.BidFee);
            Assert.Equal(87135, fill.AskRemainingQuantity);
            Assert.Equal(5434, fill.AskFee);
            Assert.Equal(123879, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_AskRemainingQuantityNull()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, BidFee = 891434, AskRemainingQuantity = null, AskFee = null, MessageSequence = 8089 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(135, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Equal(891434, fill.BidFee);
            Assert.Null(fill.AskRemainingQuantity);
            Assert.Null(fill.AskFee);
            Assert.Equal(8089, fill.MessageSequence);
        }
    }
}
