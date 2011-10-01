using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Continuum.IO;

namespace Continuum
{
    public static class ByteReader
    {
        public static byte[] Read(IStream stream, long length)
        {
            var buffer = new byte[length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static byte[] Read(Stream stream, long length)
        {
            var buffer = new byte[length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static byte[] ReadToEnd(IStream stream)
        {
            var buffer = new byte[stream.Length - stream.Position];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static byte[] ReadToEnd(Stream stream)
        {
            var buffer = new byte[stream.Length - stream.Position];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
