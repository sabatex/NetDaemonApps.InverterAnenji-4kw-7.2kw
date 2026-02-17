// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names

using Microsoft.Extensions.Options;
using NetDaemon.Extensions.MqttEntityManager;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace InverterAnenji;


[NetDaemonApp]
public class Anenji4000App : IAsyncInitializable
{
    readonly ILogger<Anenji4000App> _logger;
    readonly int localPort = 8899;
    readonly int haInternalLocalPort = 10000;
    IPEndPoint WiFiAdapterEndPoint = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 119 }), 58899);
    IPAddress haIpAddress = new IPAddress(new byte[] { 192, 168, 0, 110 });
    readonly IHaContext ha;
    readonly IMqttEntityManager entityManager;
    ushort _messageCounter = 1;
    string? serverNumber;
    InverterState inverterState = new InverterState();
    InverterState? previousValidState = null; // Зберігає попередній валідний стан
    Task? _worker = null;
    CancellationToken _cancellationToken = new CancellationToken();
    bool isDevelopment => Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";

    // Фільтри для згладжування даних - ДЛЯ ВСІХ ПАРАМЕТРІВ
    // Батарея
    private readonly ShortMedianFilter _batteryPercentageFilter = new ShortMedianFilter(3);
    private readonly FloatMedianFilter _batteryVoltageFilter = new FloatMedianFilter(3);
    private readonly FloatMedianFilter _batteryCurrentFilter = new FloatMedianFilter(3);
    private readonly ShortMedianFilter _batteryPowerFilter = new ShortMedianFilter(3);
    private readonly FloatMedianFilter _batteryAverageCurrentFilter = new FloatMedianFilter(3);
    
    // PV панелі
    private readonly FloatMedianFilter _pvVoltageFilter = new FloatMedianFilter(3);
    private readonly FloatMedianFilter _pvCurrentFilter = new FloatMedianFilter(3);
    private readonly ShortMedianFilter _pvPowerFilter = new ShortMedianFilter(3);
    private readonly ShortMedianFilter _pvChargingPowerFilter = new ShortMedianFilter(3);
    private readonly FloatMedianFilter _pvChargingAverageCurrentFilter = new FloatMedianFilter(3);
    
    // Електромережа (Grid)
    private readonly FloatMedianFilter _gridVoltageFilter = new FloatMedianFilter(3);
    private readonly FloatMedianFilter _gridFrequencyFilter = new FloatMedianFilter(3);
    private readonly ShortMedianFilter _gridLoadPowerFilter = new ShortMedianFilter(3);
    
    // Інвертор
    private readonly FloatMedianFilter _inverterVoltageFilter = new FloatMedianFilter(3);
    private readonly FloatMedianFilter _inverterCurrentFilter = new FloatMedianFilter(3);
    private readonly FloatMedianFilter _inverterFrequencyFilter = new FloatMedianFilter(3);
    private readonly ShortMedianFilter _inverterPowerFilter = new ShortMedianFilter(3);
    private readonly ShortMedianFilter _inverterChargingPowerFilter = new ShortMedianFilter(3);
    private readonly FloatMedianFilter _inverterChargingAverageCurrentFilter = new FloatMedianFilter(3);
    
    // Вихід (Output)
    private readonly FloatMedianFilter _outputVoltageFilter = new FloatMedianFilter(3);
    private readonly FloatMedianFilter _outputCurrentFilter = new FloatMedianFilter(3);
    private readonly FloatMedianFilter _outputFrequencyFilter = new FloatMedianFilter(3);
    private readonly ShortMedianFilter _outputPowerFilter = new ShortMedianFilter(3);
    private readonly ShortMedianFilter _outputApparentPowerFilter = new ShortMedianFilter(3);
    
    // Температура (МАКСИМАЛЬНЕ вікно - температура змінюється дуже повільно)
    private readonly ShortMedianFilter _dcdcTemperatureFilter = new ShortMedianFilter(21);
    private readonly ShortMedianFilter _inverterTemperatureFilter = new ShortMedianFilter(21);
    
    // Інше
    private readonly ShortMedianFilter _loadPercentageFilter = new ShortMedianFilter(3);
    
    // Валідатор даних
    private readonly InverterDataValidator _dataValidator;


    async Task<short> ReadRegisterAsync(Socket con, ushort funcCode)
    {
        if (string.IsNullOrEmpty(serverNumber))
            throw new InvalidOperationException("Server number is not initialized");
            
        byte[] buffer = new byte[64];
        var query = new QueryModBus() { TID = _messageCounter, DevCode = ushort.Parse(serverNumber), Count = 1, RegisterId = funcCode };
        var message = query.GetBytes().ToArray();

        try
        {
            await con.SendAsync(message).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Do not send message with error:{ex.Message}");
        }

        _logger.LogInformation($"Exchange iteration {_messageCounter}");
        int bytes = 0;
        try
        {
            bytes = await con.ReceiveAsync(buffer).WaitAsync(TimeSpan.FromSeconds(5));
 
        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Do not recesive message with error:{ex.Message}");
        }
        
        if (bytes == 0)throw new Exception($"Responce is empty for {funcCode}(1)");

        MemoryStream stream = new MemoryStream(buffer);
        var responceHeader = new ResponceHeader(stream);
        return stream.GetInt16();
    }

    async Task WriteRegisterAsync(Socket con, ushort registerId, short data)
    {
        if (string.IsNullOrEmpty(serverNumber))
            throw new InvalidOperationException("Server number is not initialized");
            
        byte[] buffer = new byte[64];
        var query = new QueryModBus() { TID = _messageCounter, DevCode = ushort.Parse(serverNumber), Size = 0x0d, RegisterId = registerId, FuncCode = 4,Function=0x10 };
        var dataB = BitConverter.GetBytes(data).Reverse().ToArray();
        
        
        var message = query.GetBytes(new byte[] { 2, dataB[0], dataB[1] }).ToArray();

        try
        {
            await con.SendAsync(message).WaitAsync(TimeSpan.FromSeconds(5));
            _logger.LogInformation($"Exchange iteration {_messageCounter}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Do not send message with error:{ex.Message}");
        }
        int bytes = 0;
        try
        {
            bytes = await con.ReceiveAsync(buffer).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Do not recesive message with error:{ex.Message}");
        }

        if (bytes == 0) throw new Exception($"Responce is empty for {registerId}(1)");
        
        MemoryStream stream = new MemoryStream(buffer);
        var responceHeader = new ResponceHeader(stream);
        //var result = stream.GetUInt16(); 
        //return responceHeader.Count;
 
    }


    async Task ReadRegisters(Socket con)
    {
        if (string.IsNullOrEmpty(serverNumber))
            throw new InvalidOperationException("Server number is not initialized");
            
        byte[] buffer = new byte[512];
        int bytes = 0;
        var query = new QueryModBus() { TID = _messageCounter, DevCode = ushort.Parse(serverNumber), Count = 34, RegisterId = 201 };
        var message = query.GetBytes().ToArray();
        try
        {
            await con.SendAsync(message).WaitAsync(TimeSpan.FromSeconds(5));

        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Do not send message read registers 201(34) with error:{ex.Message}");
        }
        _logger.LogTrace($"Succesful send query for registers 201(34)");

        try
        {
            bytes = await con.ReceiveAsync(buffer).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Do not recesive message read registers 201(34) with error:{ex.Message}");
        }

        if (bytes == 0) throw new Exception($"Responce is empty for 201(34)");

        // Створюємо тимчасовий стан для нових даних
        var newState = new InverterState();
        
        MemoryStream stream = new MemoryStream(buffer);
        var responceHeader = new ResponceHeader(stream);
        
        // Читаємо всі дані з інвертора
        newState.WorkingMode = (WorkingMode)stream.GetUInt16();
        newState.GridVoltage = (float)stream.GetInt16() / 10;
        newState.GridFrequency = (float)stream.GetInt16() / 100;
        newState.GridLoadPower = stream.GetInt16();
        newState.InverterVoltage = (float)stream.GetInt16() / 10;
        newState.InverterCurrent = (float)stream.GetInt16() / 10;
        newState.InverterFrequency = (float)stream.GetInt16() / 100;
        newState.InverterPower = stream.GetInt16();
        newState.InverterChargingPower = stream.GetInt16();
        newState.OutputVoltage = (float)stream.GetInt16() / 10;
        newState.OutputCurrent = (float)stream.GetInt16() / 10;
        newState.OutputFrequency = (float)stream.GetInt16() / 100;
        newState.OutputPower = stream.GetInt16();
        newState.OutputApparentPower = stream.GetInt16();
        newState.BatteryVoltage = (float)stream.GetInt16() / 10;
        newState.BatteryCurrent = (float)stream.GetInt16() / 10;
        newState.BatteryPower = stream.GetInt16();
        var r = stream.GetInt16();
        newState.PVVoltage = (float)stream.GetInt16() / 10;
        newState.PVCurrent = (float)stream.GetInt16() / 10;
        r = stream.GetInt16();
        r = stream.GetInt16();
        newState.PVPower = stream.GetInt16();
        newState.PVChargingPower = stream.GetInt16();
        newState.LoadPercentage = stream.GetInt16();
        newState.DCDCTemperature = stream.GetInt16();
        newState.InverterTemperature = stream.GetInt16();
        r = stream.GetInt16();
        newState.BatteryPercentage = stream.GetInt16();
        r = stream.GetInt16();
        r = stream.GetInt16();
        newState.BatteryAverageCurrent = (float)stream.GetInt16() / 10;
        newState.InverterChargingAverageCurrent = (float)stream.GetInt16() / 10;
        newState.PVChargingAverageCurrent = (float)stream.GetInt16();

        // Зберігаємо сирі значення для логування
        var rawBatteryPercentage = newState.BatteryPercentage;
        var rawBatteryVoltage = newState.BatteryVoltage;
        var rawDCDCTemperature = newState.DCDCTemperature;
        var rawInverterTemperature = newState.InverterTemperature;

        // Застосовуємо медіанні фільтри до ВСІХ параметрів
        
        // Батарея
        newState.BatteryPercentage = _batteryPercentageFilter.Filter(newState.BatteryPercentage);
        newState.BatteryVoltage = _batteryVoltageFilter.Filter(newState.BatteryVoltage);
        newState.BatteryCurrent = _batteryCurrentFilter.Filter(newState.BatteryCurrent);
        newState.BatteryPower = _batteryPowerFilter.Filter(newState.BatteryPower);
        newState.BatteryAverageCurrent = _batteryAverageCurrentFilter.Filter(newState.BatteryAverageCurrent);
        
        // PV панелі
        newState.PVVoltage = _pvVoltageFilter.Filter(newState.PVVoltage);
        newState.PVCurrent = _pvCurrentFilter.Filter(newState.PVCurrent);
        newState.PVPower = _pvPowerFilter.Filter(newState.PVPower);
        newState.PVChargingPower = _pvChargingPowerFilter.Filter(newState.PVChargingPower);
        newState.PVChargingAverageCurrent = _pvChargingAverageCurrentFilter.Filter(newState.PVChargingAverageCurrent);
        
        // Електромережа
        newState.GridVoltage = _gridVoltageFilter.Filter(newState.GridVoltage);
        newState.GridFrequency = _gridFrequencyFilter.Filter(newState.GridFrequency);
        newState.GridLoadPower = _gridLoadPowerFilter.Filter(newState.GridLoadPower);
        
        // Інвертор
        newState.InverterVoltage = _inverterVoltageFilter.Filter(newState.InverterVoltage);
        newState.InverterCurrent = _inverterCurrentFilter.Filter(newState.InverterCurrent);
        newState.InverterFrequency = _inverterFrequencyFilter.Filter(newState.InverterFrequency);
        newState.InverterPower = _inverterPowerFilter.Filter(newState.InverterPower);
        newState.InverterChargingPower = _inverterChargingPowerFilter.Filter(newState.InverterChargingPower);
        newState.InverterChargingAverageCurrent = _inverterChargingAverageCurrentFilter.Filter(newState.InverterChargingAverageCurrent);
        
        // Вихід
        newState.OutputVoltage = _outputVoltageFilter.Filter(newState.OutputVoltage);
        newState.OutputCurrent = _outputCurrentFilter.Filter(newState.OutputCurrent);
        newState.OutputFrequency = _outputFrequencyFilter.Filter(newState.OutputFrequency);
        newState.OutputPower = _outputPowerFilter.Filter(newState.OutputPower);
        newState.OutputApparentPower = _outputApparentPowerFilter.Filter(newState.OutputApparentPower);
        
        // Температура - спеціальна обробка для усунення всіх викидів
        bool tempIsInvalid = false;
        short rawDCDCTemp = newState.DCDCTemperature;
        short rawInvTemp = newState.InverterTemperature;
        
        // Перевірка 1: Нульові або від'ємні значення
        if (newState.DCDCTemperature <= 0 || newState.InverterTemperature <= 0)
        {
            _logger.LogWarning($"Invalid temperature detected (zero or negative): DCDC={newState.DCDCTemperature}°C, Inv={newState.InverterTemperature}°C");
            tempIsInvalid = true;
        }
        
        // Перевірка 2: Аномально низькі значення (менше 10°C малоймовірно для працюючого інвертора)
        if (!tempIsInvalid && (newState.DCDCTemperature < 10 || newState.InverterTemperature < 10))
        {
            _logger.LogWarning($"Suspiciously low temperature: DCDC={newState.DCDCTemperature}°C, Inv={newState.InverterTemperature}°C");
            tempIsInvalid = true;
        }
        
        // Якщо температура невалідна - використовуємо попередню БЕЗ фільтрації
        if (tempIsInvalid && previousValidState != null)
        {
            _logger.LogInformation($"Using previous temperature values: DCDC={previousValidState.DCDCTemperature}°C, Inv={previousValidState.InverterTemperature}°C");
            newState.DCDCTemperature = previousValidState.DCDCTemperature;
            newState.InverterTemperature = previousValidState.InverterTemperature;
        }
        else
        {
            // Спочатку застосовуємо медіанний фільтр
            newState.DCDCTemperature = _dcdcTemperatureFilter.Filter(newState.DCDCTemperature);
            newState.InverterTemperature = _inverterTemperatureFilter.Filter(newState.InverterTemperature);
            
            // Перевірка 3: Різкі стрибки після фільтрації
            if (previousValidState != null)
            {
                var dcdcChange = Math.Abs(newState.DCDCTemperature - previousValidState.DCDCTemperature);
                var invChange = Math.Abs(newState.InverterTemperature - previousValidState.InverterTemperature);
                
                if (dcdcChange > 5 || invChange > 5)
                {
                    _logger.LogWarning($"Temperature changed too rapidly after filtering: DCDC Δ{dcdcChange}°C ({previousValidState.DCDCTemperature}°C→{newState.DCDCTemperature}°C), " +
                                     $"Inv Δ{invChange}°C ({previousValidState.InverterTemperature}°C→{newState.InverterTemperature}°C). Using previous values.");
                    // Використовуємо попередні значення
                    newState.DCDCTemperature = previousValidState.DCDCTemperature;
                    newState.InverterTemperature = previousValidState.InverterTemperature;
                }
            }
        }
        
        // Інше
        newState.LoadPercentage = _loadPercentageFilter.Filter(newState.LoadPercentage);

        // Застосовуємо тільки медіанну фільтрацію (валідація вимкнена)
        // Медіанний фільтр ефективно усуває викиди без складної валідації
        
        // Зберігаємо відфільтрований стан як попередній для наступного разу
        if (previousValidState == null)
        {
            _logger.LogInformation($"First reading accepted: Battery={newState.BatteryPercentage}%/{newState.BatteryVoltage}V");
        }
        previousValidState = newState.Clone();
            
            // Логуємо якщо були значні відмінності після фільтрації
            if (Math.Abs(rawBatteryPercentage - newState.BatteryPercentage) > 2 ||
                Math.Abs(rawBatteryVoltage - newState.BatteryVoltage) > 0.5f ||
                Math.Abs(rawDCDCTemperature - newState.DCDCTemperature) > 3 ||
                Math.Abs(rawInverterTemperature - newState.InverterTemperature) > 3)
            {
                _logger.LogDebug($"Median filter smoothed values: Battery {rawBatteryPercentage}%→{newState.BatteryPercentage}%, " +
                               $"Voltage {rawBatteryVoltage}V→{newState.BatteryVoltage}V, " +
                               $"Temp DCDC {rawDCDCTemperature}°C→{newState.DCDCTemperature}°C, " +
                               $"Temp Inv {rawInverterTemperature}°C→{newState.InverterTemperature}°C");
            }
        

        // Копіюємо дані в основний стан
        inverterState = newState;

        await inverterState.SendMQTT(entityManager);
        _messageCounter++;
        
        try
        {
            if (inverterState.OutputPriority == inverterState.OutputPriorityHA)
            {
                inverterState.OutputPriority = (OutputPriority)(await ReadRegisterAsync(con, 301));
                inverterState.OutputPriorityHA = inverterState.OutputPriority;
            }
            else
            {
                await WriteRegisterAsync(con, 301, (short)inverterState.OutputPriorityHA);
                inverterState.OutputPriority = inverterState.OutputPriorityHA;

            }
        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Do not recesive register 301:{ex.Message}");
        }
        _messageCounter++;
        try
        {
            if (inverterState.BatteryChargingPriority == inverterState.BatteryChargingPriorityHA)
            {
                inverterState.BatteryChargingPriority = (BatteryChargingPriority)(await ReadRegisterAsync(con, 331));
                inverterState.BatteryChargingPriorityHA = inverterState.BatteryChargingPriority;
            }
            else
            {
                await WriteRegisterAsync(con, 331,(short)inverterState.BatteryChargingPriorityHA);
                inverterState.BatteryChargingPriority =inverterState.BatteryChargingPriorityHA;
            }
        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Do not recesive register 331:{ex.Message}");
        }


        await entityManager.SetStateAsync("select.inverter_anenji4000", JsonSerializer.Serialize(new { output_priority = inverterState.OutputPriority.ToString(), battery_charging_priority=inverterState.BatteryChargingPriority.ToString() }));

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
                    var message = $"set>server={haIpAddress}:{localPort};";
                    _logger.LogInformation(message);
                    udp.Send(Encoding.ASCII.GetBytes(message));
                    var result = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
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
            using (var listner = new TcpListener(new IPEndPoint(IPAddress.Any, isDevelopment ? localPort : haInternalLocalPort)))
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
                            await ReadRegisters(socket).WaitAsync(TimeSpan.FromSeconds(3));
                            _logger.LogInformation("End read registers");
                        }
                        catch (TimeoutException)
                        {
                            _logger.LogError("Breack connectin time out 2 sec");
                            success = false;
                            socket.Close();
                        }

                        catch (Exception ex)
                        {
                           _logger.LogError($"Bad read  result {ex.Message}");
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
            var exch = ExchangeServer(_cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(10));
        });
    }

    public Anenji4000App(IHaContext ha, ILogger<Anenji4000App> logger, IMqttEntityManager entityManager)
    {
        _logger = logger;
        this.ha = ha;
        this.entityManager = entityManager;
        _dataValidator = new InverterDataValidator(_logger);
        
        if (isDevelopment)
        {
            haIpAddress = new IPAddress(new byte[] { 192, 168, 0, 110 });
        }
    }



}