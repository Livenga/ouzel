namespace ouzel.Printer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;


/// <summary></summary>
public sealed class LocalPrinter
{
    /// <summary></summary>
    public string  DeviceId { get; }

    /// <summary></summary>
    public string  Name { get; }

    /// <summary></summary>
    public string  DriverName { get; }

    /// <summary></summary>
    public string? Comment { get; }


    /// <summary></summary>
    private LocalPrinter(
            string  deviceId,
            string  name,
            string  driverName,
            string? comment)
    {
        DeviceId   = deviceId;
        Name       = name;
        DriverName = driverName;
        Comment    = comment;
    }


    /// <summary></summary>
    public static IEnumerable<LocalPrinter> GetAll()
    {
        using var search = new ManagementObjectSearcher("select * from Win32_Printer");

        return search.Get()
            .Cast<ManagementObject>()
            .Select(mo => new LocalPrinter(
                        deviceId:   mo["DeviceID"]   as string ?? string.Empty,
                        name:       mo["Name"]       as string ?? throw new NullReferenceException(),
                        driverName: mo["DriverName"] as string ?? throw new NullReferenceException(),
                        comment:    mo["Comment"]    as string))
            .ToArray();
    }
}
