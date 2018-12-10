using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace HyperLib
{
    public static class Extensions
    {
        public static int Size(this Enumerations currentDataType)
        {
            switch (currentDataType)
            {
                case Enumerations.uint8:
                    return sizeof(byte);
                case Enumerations.int16:
                    return sizeof(Int16);
                case Enumerations.int32:
                    return sizeof(Int32);
                case Enumerations.single:
                    return sizeof(Single);
                case Enumerations.envidouble:
                    return sizeof(double);
                case Enumerations.uint32:
                    return sizeof(UInt32);
                case Enumerations.int64:
                    return sizeof(Int64);
                case Enumerations.uint16:
                    return sizeof(UInt16);
                default:
                    return sizeof(double);
            }
        }
        
    }
}
