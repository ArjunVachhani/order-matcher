using System;

namespace OrderMatcher
{
    interface ITradeLogger : IDisposable
    {
        void Log(byte[] bytes);
        void Flush();
    }
}
