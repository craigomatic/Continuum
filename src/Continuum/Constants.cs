using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum
{
    public class Constants
    {
        public static readonly byte[] CONTINUUM_HEADER_SIGNATURE = new byte[16] 
        {
            (byte)99, //these bytes say CONTINUUM in ascii 
            (byte)111, 
            (byte)110, 
            (byte)116, 
            (byte)105, 
            (byte)110, 
            (byte)117, 
            (byte)117,
            (byte)109, 
            byte.MinValue, //7 bytes reserved for future use
            byte.MinValue,
            byte.MinValue,
            byte.MinValue,
            byte.MinValue,
            byte.MinValue,
            byte.MinValue
        };

        public static readonly byte[] CONTINUUM_VERSION = new byte[4]
        {
            (byte)2,
            (byte)0,
            (byte)0,
            (byte)0
        };
    }
}
