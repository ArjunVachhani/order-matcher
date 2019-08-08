using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OrderMatcher
{
    class TradeLogger 
    {
        private BlockingCollection<byte[]> _logInput;
        private BlockingCollection<byte[]> _internalBuffer;
        private const int CHUNK_SIZE = 4096;
        private object _lock;
        private bool _disposed;
        private byte[] _buffer;
        private int _offset;
        private int BufferRemaining => _buffer.Length - _offset;
        private readonly FileStream _filestream;
        Task _logReader;
        Task _logSaver;
        public bool SaveRemaining => _internalBuffer.Count > 0 || _logInput.Count > 0 || _buffer != null;
        public TradeLogger(string filePath, BlockingCollection<byte[]> logInput)
        {
            _logInput = logInput;
            _lock = new object();
            _disposed = false;
            _filestream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _internalBuffer = new BlockingCollection<byte[]>();
            _offset = 0;
            _logReader = new Task(LogReader);
            _logSaver = new Task(() => LogSaver());
            _logReader.Start();
            _logSaver.Start();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logReader.Wait();
                _logSaver.Wait();
                _disposed = true;
            }
        }

        public void LogReader()
        {
            while (!_logInput.IsCompleted)
            {
                var bytes = _logInput.Take();
                if (bytes.Length == 0)
                {
                    if (_buffer != null)
                    {
                        byte[] data = new byte[_offset];
                        Array.Copy(_buffer, 0, data, 0, _offset);
                        _buffer = null;
                        _internalBuffer.Add(data);
                    }
                    else
                    {
                        _filestream.Flush();
                    }
                }
                else
                {
                    int inputOffset = 0;
                    while (inputOffset < bytes.Length)
                    {
                        int length = bytes.Length < BufferRemaining ? bytes.Length : BufferRemaining;
                        WriteToBuffer(bytes, inputOffset, length);
                        inputOffset += length;
                    }
                }
            }
        }

        private void WriteToBuffer(byte[] bytes, int inputOffset, int length)
        {
            if (_buffer == null)
                _buffer = new byte[CHUNK_SIZE];

            Array.Copy(bytes, inputOffset, _buffer, _offset, length);
            _offset += length;
            if (_offset == CHUNK_SIZE)
            {
                _internalBuffer.Add(_buffer);
                _offset = 0;
                _buffer = null;
            }
        }

        private void LogSaver()
        {
            while (!_internalBuffer.IsCompleted)
            {
                try
                {
                    var chunk = _internalBuffer.Take();
                    _filestream.Write(chunk, 0, chunk.Length);
                    if (chunk.Length != CHUNK_SIZE)
                        _filestream.Flush();
                }
                catch (Exception)
                {
                    //todo 
                }
            }
            _filestream.Flush();
            _filestream.Dispose();
        }
    }
}
