using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.Inverter.Anenji_4_7_WiFi;

public class ResponceHeader
{
    public UInt16 TID { get; set; }        // counter
    public UInt16 DevCode { get; set; }    // global dev code
    public UInt16 Size { get; set; }       //
    public byte DevAdr { get; set; }
    public byte FuncCode { get; set; }



    public byte DeviceId { get; set; }
    public byte Function { get; set; }
    //public UInt16 RegisterId { get; set; }
    public byte Count { get; set; }


    public ResponceHeader(Stream stream)
    {
        TID = stream.GetUInt16();                           //0
        DevCode = stream.GetUInt16();     //2
        Size = stream.GetUInt16();        //4 
        DevAdr = (byte)stream.ReadByte();                     //6 
        FuncCode = (byte)stream.ReadByte();                   //7 
        DeviceId = (byte)stream.ReadByte();                   //8
        Function = (byte)stream.ReadByte();                   //9
        Count = (byte)stream.ReadByte();                      //12;

    }

}
