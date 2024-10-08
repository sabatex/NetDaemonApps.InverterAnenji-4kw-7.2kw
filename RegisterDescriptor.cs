using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.Inverter.Anenji_4_7_WiFi;

public class RegisterDescriptor
{
    public ushort Func { get; set; }
    public string? MqttSubacription { get; set; }
    public Type RegisterType { get; set; } = typeof(ushort);
    public string? Description { get; set; }
    public Type ResultType { get; set; } = typeof(ushort);
    public int Divider { get; set; } = 1;

    public object? Options { get; set; }
    public string? DeviceClass { get; set; }

    public string? Result { get; set; }


    public static Dictionary<ushort, RegisterDescriptor> Registers = new Dictionary<ushort, RegisterDescriptor>()
    {
        {201, new RegisterDescriptor { Func = 201, RegisterType = typeof(WorkingMode), MqttSubacription = "Working_Mode", Description = "Working Mode" } },
        {202, new RegisterDescriptor
        {
            Func = 202,
            RegisterType = typeof(short),
            MqttSubacription = "grid_voltage",
            Description = "Grid voltage",
            ResultType = typeof(float),
            Divider = 10,
            Options = new { unit_of_measurement = "V" },
            DeviceClass = "Power"
        } },
        {203, new RegisterDescriptor
        {
            Func = 203,
            RegisterType = typeof(short),
            MqttSubacription = "grid_frequency",
            Description = "Grid Frequency",
            ResultType = typeof(float),
            Divider = 100,
            Options = new { unit_of_measurement = "Hz" },
            DeviceClass = "Power"

        } },
        {204, new RegisterDescriptor
        {
            Func = 204,
            RegisterType = typeof(short),
            MqttSubacription = "grid_power",
            Description = "Grid Load power",
            ResultType = typeof(short),
            Divider = 1,
            Options = new { unit_of_measurement = "w" },
            DeviceClass = "Power"

        } }

    };

    public static ushort GetUshort(Stream stream)
    {
        var hi = (ushort)(stream.ReadByte() << 8);
        var lo = (ushort)stream.ReadByte();
        return (ushort)(hi + lo);
    }
    static short GetShort(Stream stream)
    {
        var hi = (ushort)(stream.ReadByte() << 8);
        var lo = (ushort)stream.ReadByte();
        return (short)(hi + lo);
    }

    static string GetString(ushort value, int divider) => divider == 1 ? value.ToString() : ((double)value / divider).ToString();
    static string GetString(short value, int divider) => divider == 1 ? value.ToString() : ((double)value / divider).ToString();

    public static (RegisterDescriptor, string) GetValue(ushort Func, Stream stream)
    {
        var descriptor = Registers[Func];
        if (descriptor.RegisterType == typeof(ushort))
        {
            return (descriptor, GetString(GetUshort(stream), descriptor.Divider));
        }

        if (descriptor.RegisterType == typeof(short))
        {
            return (descriptor, GetString(GetUshort(stream), descriptor.Divider));
        }
        if (descriptor.RegisterType.IsEnum)
        {
            var r = GetUshort(stream);
            var e = Activator.CreateInstance(descriptor.RegisterType, r);
            return (descriptor, e.ToString());
        }


        throw new NotImplementedException();
    }


    public static IEnumerable<RegisterDescriptor> Registered()
    {
        yield return new RegisterDescriptor { Func = 201, RegisterType = typeof(WorkingMode), MqttSubacription = "Working_Mode", Description = "Working Mode" };
        yield return new RegisterDescriptor
        {
            Func = 202,
            RegisterType = typeof(short),
            MqttSubacription = "Main_voltage",
            Description = "Effective mains voltage",
            ResultType = typeof(float),
            Divider = 10
        };
        yield return new RegisterDescriptor
        {
            Func = 203,
            RegisterType = typeof(short),
            MqttSubacription = "Main_Frequency",
            Description = "Mains Frequency",
            ResultType = typeof(float),
            Divider = 100

        };

    }




}
