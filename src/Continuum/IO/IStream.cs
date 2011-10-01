using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum.IO
{
    /// <summary>
    /// Represents a binary data source that can be read from and written to
    /// </summary>
    public interface IStream : IDisposable
    {
        bool CanRead { get; }
        bool CanWrite { get; }
        long Position { get; set; }
        long Length { get; }

        int ReadByte();
        int Read(byte[] buffer, int offset, int count);

        void WriteByte(byte value);
        void Write(byte[] buffer, int offset, int count);

        void Close(); 
    }
}
