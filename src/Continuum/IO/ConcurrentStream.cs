using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Continuum.IO
{
    /// <summary>
    /// A thread-safe stream that supports writers from multiple threads
    /// </summary>
    public class ConcurrentStream : IStream
    {
        public bool CanRead
        {
            get { return _Stream.CanRead; }
        }

        public bool CanWrite
        {
            get { return _Stream.CanWrite; }
        }

        public long Position
        {
            get { return _Stream.Position; }
            set 
            {
                lock (_Lock)
                {
                    _Stream.Position = value;
                }
            }
        }

        public long Length
        {
            get 
            {
                lock (_Lock)
                {
                    return _Stream.Length;
                }
            }
        }

        private object _Lock;
        private Stream _Stream;

        public ConcurrentStream(Stream stream)
        {
            _Stream = stream;
            _Lock = new object();
        }

        public int ReadByte()
        {
            lock (_Lock)
            {
                return _Stream.ReadByte();
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            lock (_Lock)
            {
                return _Stream.Read(buffer, offset, count);
            }
        }

        public void WriteByte(byte value)
        {
            lock (_Lock)
            {
                _Stream.WriteByte(value);
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            lock (_Lock)
            {
                _Stream.Write(buffer, offset, count);
            }
        }

        public void Close()
        {
            lock (_Lock)
            {
                _Stream.Close();
            }
        }

        public void Dispose()
        {
            lock (_Lock)
            {
                _Stream.Dispose();
            }
        }
    }
}
