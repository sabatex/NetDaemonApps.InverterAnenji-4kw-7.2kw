##Updated filters, so you need to use instruction below. 
##Youtube video will be added

# OpenESS (Anenji 4kw/7.2kw)

An app that exports electricity consumption and other metrics from Chinese solar inverters to Home Assistant using the WiFi adapter attached to the inverters. [SmartESS mobile app](https://play.google.com/store/apps/details?id=com.eybond.smartclient.ess).


Thanks [Alexey Denisov](https://github.com/alexeyden/openess)

## Installation and configuration

1. Install [NetDaemon](https://netdaemon.xyz/) to HomeAssistant

2. Install MQTT.

3. Create in you personal computer template project NetDaemon or use exist.

``` powershell
dotnet new --install NetDaemon.Templates.Project
mkdir NetDaemonApps
cd NetDaemonApps
dotnet new nd-project
```

  If you use the NetDaemon project template you will already have the entity manager available and you can skip this setup.
  To set up the entity manager manually you should:
  Include the NetDaemon.Extensions.MqttEntityManager NuGet package:
``` powershell
dotnet add package NetDaemon.Extensions.Mqtt
```

Update your appsettings.json with a new section:

``` C#
{
  "Logging": {
    // more config
  },
  "NetDaemon": {
    // more config
  },
  "Mqtt": {
    "Host": "ENTER YOUR IP TO your MQTT Broker",
    "Port": "ENTER YOUR PORT TO your MQTT Broker (default 1883)",
    "UserName": "Enter your MQTT broker USERNAME",
    "Password": "Enter your MQTT broker PASSWORD",
    "DiscoveryPrefix": "MQTT broker discovery rrefix (default homeassistant)"
  }
}
```

4. In solution explorer navigate to folder <b>apps/HasModel</b> and create application folder <b>Anenji Inverter</b>.

5. In folder click left mouse button and select <b>add existig items</b>, choise all *.cs files from src folder,click on <b>Add</b>'s button drop-down menu and choose <b>Add as a link</b>

6. Open file <i>InverterAnenji4_7.cs</i> and correct lines.

``` C#
    IPEndPoint WiFiAdapterEndPoint = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 1, 128 }), 58899);
    IPAddress haIpAddress = new IPAddress(new byte[] { 192, 168, 1, 100 });

```

7. In the NetDaemon configuration tab in the network configuration area, change port <b>10000</b> to <b>8899</b>.

8. Modify Program.cs 
``` C#
   // Program.cs
   await Host.CreateDefaultBuilder(args)
        .UseNetDaemonAppSettings()
        .UseNetDaemonDefaultLogging()
        .UseNetDaemonRuntime()
        .UseNetDaemonTextToSpeech()
        // add for mqtt
        .UseNetDaemonMqttEntityManagement() 
        // #8
        .ConfigureServices((_, services) =>
            services
                .AddAppsFromAssembly(Assembly.GetExecutingAssembly())
                .AddNetDaemonStateManager()
                .AddNetDaemonScheduler()
                // Add next line if using code generator
                .AddHomeAssistantGenerated()
        )
        .Build()
        .RunAsync()
        .ConfigureAwait(false);
```
issue: [MQTT error #8](https://github.com/sabatex/NetDaemonApps.InverterAnenji-4kw-7.2kw/issues/8)

9. Run debug or publish to HA.

 

## P.S.

The Application, once launched, automatically registers the <b> OffGrid Inverter Anenji </b> device with all registers [201-234]( https://github.com/HotNoob/PythonProtocolGateway/blob/main/protocols/eg4/eg4_3000ehv_v1.holding_registry_map.csv ).

After successful integration to Home Assistant recomendation close access to internet for WiFi doungle. (Increases connection stability)

If someone writes a more detailed instruction or makes a video on YouTube, it would be welcomed.
 
 
