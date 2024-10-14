// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names

using NetDaemon.Extensions.MqttEntityManager;
using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace InverterAnenji;


[NetDaemonApp]
public class Anenji4000App: IAsyncInitializable
{
    readonly ILogger<Anenji4000App> _logger;
    readonly int localPort = 8899;
    readonly IHaContext ha;
    readonly  IMqttEntityManager entityManager;
    ushort _messageCounter=1;
    string? serverNumber;
    InverterState inverterState = new InverterState();
    IPEndPoint WiFiAdapterEndPoint = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 1, 128 }), 58899);
    Task? _worker = null;
    CancellationToken _cancellationToken = new CancellationToken();

    public async Task InitialConnection()
    {
        try
        {
            using (var udp = new UdpClient())
            {

                udp.Connect(WiFiAdapterEndPoint);
                var ip = new IPAddress(new byte[] { 192, 168, 1, 100 });
                var message = $"set>server={ip}:{localPort};";
                _logger.LogInformation(message);
                udp.Send(Encoding.ASCII.GetBytes(message));
                var result = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(2));
                var str = Encoding.ASCII.GetString(result.Buffer);
                if (str.Contains("rsp>server="))
                {
                    serverNumber = str.Substring(11, str.IndexOf(";") - 11);
                }
                _logger.LogInformation(str);
                udp.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Do not execute udp initializer with  error: {ex.Message}");

        }
    }


    async Task<bool> ReadRegisters(Socket con)
    {
        byte[] buffer = new byte[1024];
        ushort funcCode = 201;
        var query = new QueryModBus() { TID = _messageCounter, DevCode = ushort.Parse(serverNumber), Count = 34, RegisterId = funcCode };
        var message = query.GetBytes().ToArray();
        try
        {
            await con.SendAsync(message).WaitAsync(TimeSpan.FromSeconds(2));
            _logger.LogInformation($"Exchange iteration {_messageCounter}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Do not send message with error:{ex.Message}");
            return false;
        }
        try
        {
            var l = await con.ReceiveAsync(buffer).WaitAsync(TimeSpan.FromSeconds(2));
            if (l != 0)
            {
                MemoryStream stream = new MemoryStream(buffer);
                var responceHeader = new ResponceHeader(stream);
                inverterState.WorkingMode = (WorkingMode)stream.GetUInt16();
                inverterState.GridVoltage = (float)stream.GetInt16() / 10;
                inverterState.GridFrequency = (float)stream.GetInt16() / 100;
                inverterState.GridLoadPower = stream.GetInt16();
                inverterState.InverterVoltage = (float)stream.GetInt16() / 10;
                inverterState.InverterCurrent = (float)stream.GetInt16() / 10;
                inverterState.InverterFrequency = (float)stream.GetInt16() / 100;
                inverterState.InverterPower = stream.GetInt16();
                inverterState.InverterChargingPower = stream.GetInt16();
                inverterState.OutputVoltage = (float)stream.GetInt16() / 10;
                inverterState.OutputCurrent = (float)stream.GetInt16() / 10;
                inverterState.OutputFrequency = (float)stream.GetInt16() / 100;
                inverterState.OutputPower = stream.GetInt16();
                inverterState.OutputApparentPower = stream.GetInt16();
                inverterState.BatteryVoltage = (float)stream.GetInt16() / 10;
                inverterState.BatteryCurrent = (float)stream.GetInt16() / 10;
                inverterState.BatteryPower = stream.GetInt16();
                var r = stream.GetInt16();
                inverterState.PVVoltage = (float)stream.GetInt16() / 10;
                inverterState.PVCurrent = (float)stream.GetInt16() / 10;
                r = stream.GetInt16();
                r = stream.GetInt16();
                inverterState.PVPower = stream.GetInt16();
                inverterState.PVChargingPower = stream.GetInt16();
                inverterState.LoadPercentage = stream.GetInt16();
                inverterState.DCDCTemperature = stream.GetInt16();
                inverterState.InverterTemperature = stream.GetInt16();
                r = stream.GetInt16();
                inverterState.BatteryPercentage = stream.GetInt16();
                r = stream.GetInt16();
                r = stream.GetInt16();
                inverterState.BatteryAverageCurrent = (float)stream.GetInt16() / 10;
                inverterState.InverterChargingAverageCurrent = (float)stream.GetInt16() / 10;
                inverterState.PVChargingAverageCurrent = (float)stream.GetInt16();

                await inverterState.SendMQTT(entityManager);
                _messageCounter++;
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Do not recesive message with error:{ex.Message}");
            return false;

        }
        return false;


    }

    public async Task ExchangeServer(CancellationToken? cancellationToken = null)
    {
        do
        {
            _logger.LogTrace("Begin exchange, pause 10 sec");
            await Task.Delay(TimeSpan.FromSeconds(10));
            try
            {
                using (var udp = new UdpClient())
                {
                    try
                    {

                        udp.Connect(WiFiAdapterEndPoint);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error udp connect: {ex.Message}");
                    }
                    var ip = new IPAddress(new byte[] { 192, 168, 1, 100 });
                    var message = $"set>server={ip}:{localPort};";
                    _logger.LogInformation(message);
                    udp.Send(Encoding.ASCII.GetBytes(message));
                    var result = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(2));
                    var str = Encoding.ASCII.GetString(result.Buffer);
                    if (str.Contains("rsp>server="))
                    {
                        serverNumber = str.Substring(11, str.IndexOf(";") - 11);
                    }
                    _logger.LogInformation(str);
                    udp.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Do not execute udp initializer with  error: {ex.Message}");

            }

            Socket? socket = null;
            using (var listner = new TcpListener(new IPEndPoint(IPAddress.Any, 10000)))
            {
                try
                {
                    listner.Start();
                }
                catch (SocketException ex)
                {
                    _logger.LogError($"listner.Start(): {ex.Message}");
                    continue;
                }
                while (true)
                {
                    try
                    {
                        _logger.LogInformation("Start listner");
                        socket = await listner.AcceptSocketAsync().WaitAsync(TimeSpan.FromSeconds(60));
                        _logger.LogInformation("AcceptSocketAsync");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AcceptSocketAsync: {ex.Message}");
                        break;
                    }
                    int counter = 0;
                    bool success = true;
                    while (success)
                    {
                        counter++;
                        _logger.LogInformation("Start read registers");

                        try
                        {
                            success = await ReadRegisters(socket).WaitAsync(TimeSpan.FromSeconds(3));
                            if (success)
                            {
                                _logger.LogInformation("End read registers");
                            }
                            else
                            {
                                _logger.LogError("Bad read  result");
                                socket.Close();
                            }

                        }
                        catch (Exception ex)
                        {
                            success = false;
                            _logger.LogError("Breack connectin time out 2 sec");
                            socket.Close();

                        }
                        await Task.Delay(5000);
                    }
                }

            }


            await Task.Delay(TimeSpan.FromSeconds(10));


        } while (!(cancellationToken ?? CancellationToken.None).IsCancellationRequested);
    }


    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await inverterState.CreateSensors(entityManager);
        _worker = Task.Run(async () =>
        {
           var exch =  ExchangeServer(_cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(10));
        });
    }

    public Anenji4000App(IHaContext ha,ILogger<Anenji4000App> logger, IMqttEntityManager entityManager)
    {
        _logger = logger;
        this.ha = ha;
        this.entityManager = entityManager;
    }

    

}