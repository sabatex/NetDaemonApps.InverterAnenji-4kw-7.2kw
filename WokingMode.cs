using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.Inverter.Anenji_4_7_WiFi;

public enum WorkingMode
{
    [Display(Description = "Power On Mode")]
    PowerOn = 0,
    [Display(Description = "Standby mode")]
    Standby = 1,
    [Display(Description = "Mains mode")]
    Mains = 2,
    [Display(Description = "Off-Grid mode")]
    OffGrid = 3,
    [Display(Description = "Bypass mode")]
    Bypass = 4,
    [Display(Description = "Charging mode")]
    Charging = 5,
    [Display(Description = "Fault mode")]
    Fault = 6
}
