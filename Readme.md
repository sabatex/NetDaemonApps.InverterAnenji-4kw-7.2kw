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

4. In solution explorer navigate to folder <b>apps/HasModel</b> and create application folder <b>Anenji Inverter</b>.
5. In folder click left mouse button and select <b>add existig items</b>, choise all *.cs files from src folder,click on <b>Add</b>'s button drop-down menu and choose <b>Add as a link</b>
6. Open file <i>InverterAnenji4_7.cs</i> and correct lines.

``` C#
    IPEndPoint WiFiAdapterEndPoint = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 1, 128 }), 58899);
    IPAddress haIpAddress = new IPAddress(new byte[] { 192, 168, 1, 100 });

```
7. In the NetDaemon configuration tab in the network configuration area, change port <b>10000</b> to <b>8899</b>.

8. Run debug or publish to HA.

 

## P.S.

The Application, once launched, automatically registers the <b> OffGrid Inverter Anenji </b> device with all registers [201-234]( https://github.com/HotNoob/PythonProtocolGateway/blob/main/protocols/eg4/eg4_3000ehv_v1.holding_registry_map.csv ).

After successful integration to Home Assistant recomendation close access to internet for WiFi doundle. (Increases connection stability)
 
 
