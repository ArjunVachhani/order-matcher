using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace OrderMatcher
{
    class TradeLogger : ITradeLogger
    {
        private const int CHUNK_SIZE = 4096;
        private object _lock;
        private bool _disposed;
        private byte[] _buffer;
        private int _offset;
        private int _bufferEmpty => _buffer.Length - _offset;
        private readonly LinkedList<byte[]> _queuedBuffer;
        private readonly FileStream _filestream;
        public TradeLogger(string filePath)
        {
            _lock = new object();
            _disposed = false;
            _filestream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _queuedBuffer = new LinkedList<byte[]>();
            _buffer = new byte[CHUNK_SIZE];
            _offset = 0;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    FlushToFile(true);
                    _filestream.Dispose();
                    _disposed = true;
                }
            }
        }

        public void Flush()
        {
            FlushToFile(true);
            _filestream.Flush();
        }

        public void Log(byte[] bytes)
        {
            int inputOffset = 0;
            WriteToBuffer(BitConverter.GetBytes(bytes.Length), 0, 4);
            while (inputOffset < bytes.Length)
            {
                int length = bytes.Length < _bufferEmpty ? bytes.Length : _bufferEmpty;
                WriteToBuffer(bytes, inputOffset, length);
                inputOffset += length;
            }
        }

        private void WriteToBuffer(byte[] bytes, int inputOffset, int length)
        {
            Array.Copy(bytes, inputOffset, _buffer, _offset, length);
            _offset += length;
            if (_offset == CHUNK_SIZE)
            {
                _offset = 0;
                lock (_lock)
                {
                    _queuedBuffer.AddLast(_buffer);
                }
                _buffer = new byte[CHUNK_SIZE];
            }
        }

        private void KeepFlushing()
        {
            FlushToFile(false);
        }

        private void FlushToFile(bool returnAfterFlush)
        {
            while (true)
            {
                bool allFlushed = false;
                try
                {
                    lock (_lock)
                    {
                        if (_queuedBuffer.First != null)
                        {
                            _filestream.Write(_queuedBuffer.First.Value, 0, _queuedBuffer.First.Value.Length);
                            _queuedBuffer.RemoveFirst();
                        }
                        else
                        {
                            allFlushed = true;
                        }
                    }
                    if (allFlushed && returnAfterFlush)
                    {
                        break;
                    }
                    else if (allFlushed)
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (Exception)
                {
                    //todo 
                }
            }
        }
    }
}
