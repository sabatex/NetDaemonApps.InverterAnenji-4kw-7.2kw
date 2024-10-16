using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InverterAnenji;

public class QueryModBus
{
    public UInt16 TID = 0x5;       // counter
    public UInt16 DevCode = 1;   // global dev code
    public UInt16 Size = 0x0a;
    public byte DevAdr = 0xFF;
    public byte FuncCode = 4;
    public byte DeviceId = 1;
    public byte Function = 3;
    public UInt16 RegisterId = 204;
    public UInt16 Count = 1;

    public QueryModBus()
    {

    }
    public QueryModBus(Stream stream)
    {
        UInt16 TID = stream.GetUInt16();         //0
        ushort DevCode = stream.GetUInt16();     //2
        ushort Size = stream.GetUInt16();        //4 
        byte DevAdr = (byte)stream.ReadByte();                     //6 
        byte FuncCode = (byte)stream.ReadByte();                   //7 
        byte DeviceId = (byte)stream.ReadByte();                   //8
        byte Function = (byte)stream.ReadByte();                   //9
        UInt16 RegisterId = stream.GetUInt16();  //10
        UInt16 Count = stream.GetUInt16();       //12;
    }

    IEnumerable<byte> GetMessageFrame(byte[]? Additional_bytes)
    {
        yield return DeviceId;
        yield return Function;
        var bytes = BitConverter.GetBytes(RegisterId).Reverse().ToArray();
        yield return bytes[0];
        yield return bytes[1];
        bytes = BitConverter.GetBytes(Count).Reverse().ToArray();
        yield return bytes[0];
        yield return bytes[1];
        if (Additional_bytes != null)
        {
            foreach (byte b in Additional_bytes) yield return b;
        }

    }


    public IEnumerable<byte> GetBytes(byte[]? additionalBytes=null)
    {
        var bytes = BitConverter.GetBytes(TID).Reverse().ToArray(); ;
        yield return bytes[0];
        yield return bytes[1];
        bytes = BitConverter.GetBytes(DevCode).Reverse().ToArray(); ;
        yield return bytes[0];
        yield return bytes[1];
        bytes = BitConverter.GetBytes(Size).Reverse().ToArray(); ;
        yield return bytes[0];
        yield return bytes[1];
        yield return DevAdr;
        yield return FuncCode;
        var message = GetMessageFrame(additionalBytes);
        var CRC = ModRTU_CRC(message);
        foreach (byte b in message)
        {
            yield return b;
        }

        bytes = BitConverter.GetBytes(CRC);
        yield return bytes[0];
        yield return bytes[1];



    }
    UInt16 ModRTU_CRC(IEnumerable<byte> message)
    {
        UInt16 crc = 0xFFFF;
        foreach (var value in message)
        {
            crc ^= (UInt16)value; // XOR byte into least significant byte of crc
            for (int i = 8; i != 0; i--)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1; // Shift right and XOR 0xA001
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1; // Just shift right
                }
            }
        }
        // Note: The resulting CRC has low and high bytes swapped, so use it accordingly (or swap bytes)
        return crc;
    }

    public IEnumerable<byte> GetEmptyHeader()
    {
        var bytes = BitConverter.GetBytes(TID).Reverse().ToArray(); ;
        yield return bytes[0];
        yield return bytes[1];
        bytes = BitConverter.GetBytes(DevCode).Reverse().ToArray(); ;
        yield return bytes[0];
        yield return bytes[1];
        bytes = BitConverter.GetBytes((ushort)2).Reverse().ToArray();
        yield return bytes[0];
        yield return bytes[1];
        yield return DevAdr;
        yield return FuncCode;
    }

}
