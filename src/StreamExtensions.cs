using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InverterAnenji;

public static class StreamExtensions
{
    public static UInt16 GetUInt16(this Stream stream)
    {
        var hi = stream.ReadByte();
        if (hi == -1)
            throw new Exception("Stream ended");
        var lo = stream.ReadByte();
        if (lo == -1)
            throw new Exception("Stream ended");
        return (UInt16)((hi << 8) | lo);
    }
    public static UInt16 GetUInt16Intel(this Stream stream)
    {
        var hi = stream.ReadByte();
        if (hi == -1)
            throw new Exception("Stream ended");
        var lo = stream.ReadByte();
        if (lo == -1)
            throw new Exception("Stream ended");
        return (UInt16)((lo << 8) | hi);
    }
    public static Int16 GetInt16(this Stream stream)
    {
        var hi = stream.ReadByte();
        if (hi == -1)
            throw new Exception("Stream ended");
        var lo = stream.ReadByte();
        if (lo == -1)
            throw new Exception("Stream ended");
        return (Int16)((hi << 8) | lo);
    }

}
