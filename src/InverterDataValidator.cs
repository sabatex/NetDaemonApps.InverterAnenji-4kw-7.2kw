// Copyright (c) 2024 Serhiy Lakas
// https://sabatex.github.io

using System;
using System.Collections.Generic;
using System.Linq;

namespace InverterAnenji;

/// <summary>
/// Валідатор даних інвертора для фільтрації аномальних значень
/// </summary>
public class InverterDataValidator
{
    private readonly ILogger? _logger;

    // Мінімальні допустимі значення для LiFePO4 16S (48V система)
    private const float MIN_BATTERY_VOLTAGE = 38.0f;  // 16S × 2.5V = 40V (розряджена) - запас
    private const float MAX_BATTERY_VOLTAGE = 62.0f;  // 16S × 3.65V = 58.4V (повна зарядка) + більший запас
    private const short MIN_BATTERY_PERCENTAGE = 0;
    private const short MAX_BATTERY_PERCENTAGE = 100;
    
    private const float MIN_PV_VOLTAGE = 0.0f;
    private const float MAX_PV_VOLTAGE = 200.0f;  // Збільшено запас
    
    private const float MIN_GRID_VOLTAGE = 170.0f;  // 220V ± більше відхилення
    private const float MAX_GRID_VOLTAGE = 270.0f;
    
    private const float MIN_FREQUENCY = 48.0f;  // 50Hz ± відхилення
    private const float MAX_FREQUENCY = 52.0f;

    // Максимальні дозволені зміни за один цикл (5 секунд) - послаблено для стабільності
    private const short MAX_BATTERY_PERCENTAGE_CHANGE = 25;  // 25% за 5 секунд
    private const float MAX_BATTERY_VOLTAGE_CHANGE = 8.0f;   // 8V за 5 секунд (для 48V системи)
    private const short MAX_POWER_CHANGE = 3500;             // 3.5kW за 5 секунд
    private const short MAX_TEMPERATURE_CHANGE = 15;         // 15°C за 5 секунд
    
    // Температурні межі
    private const short MIN_TEMPERATURE = -20;   // мінімальна реалістична температура
    private const short MAX_TEMPERATURE = 100;   // максимальна температура (перегрів)

    public InverterDataValidator(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Перевіряє, чи є нові дані валідними порівняно з попередніми
    /// </summary>
    public bool IsValidReading(InverterState newState, InverterState? previousState)
    {
        // Якщо немає попереднього стану, приймаємо перші дані після базової перевірки
        if (previousState == null || previousState.BatteryPercentage == 0)
        {
            return IsValidInitialReading(newState);
        }

        var issues = new List<string>();

        // Перевірка батареї
        if (!IsValidBatteryData(newState, previousState, issues))
            return LogAndReturn(false, issues, newState);

        // Перевірка PV панелей
        if (!IsValidPVData(newState, previousState, issues))
            return LogAndReturn(false, issues, newState);

        // Перевірка електромережі
        if (!IsValidGridData(newState, previousState, issues))
            return LogAndReturn(false, issues, newState);

        // Перевірка потужності
        if (!IsValidPowerData(newState, previousState, issues))
            return LogAndReturn(false, issues, newState);

        // Перевірка температури
        if (!IsValidTemperatureData(newState, previousState, issues))
            return LogAndReturn(false, issues, newState);

        return true;
    }

    private bool IsValidInitialReading(InverterState state)
    {
        // Базова перевірка для першого читання
        if (state.BatteryVoltage < MIN_BATTERY_VOLTAGE || state.BatteryVoltage > MAX_BATTERY_VOLTAGE)
        {
            _logger?.LogWarning($"Invalid initial battery voltage: {state.BatteryVoltage}V (valid range: {MIN_BATTERY_VOLTAGE}-{MAX_BATTERY_VOLTAGE}V)");
            return false;
        }

        if (state.BatteryPercentage < MIN_BATTERY_PERCENTAGE || state.BatteryPercentage > MAX_BATTERY_PERCENTAGE)
        {
            _logger?.LogWarning($"Invalid initial battery percentage: {state.BatteryPercentage}% (valid range: {MIN_BATTERY_PERCENTAGE}-{MAX_BATTERY_PERCENTAGE}%)");
            return false;
        }
        
        // Перше читання валідне
        _logger?.LogInformation($"Initial reading accepted: Battery={state.BatteryPercentage}%/{state.BatteryVoltage}V");
        return true;
    }

    private bool IsValidBatteryData(InverterState newState, InverterState previousState, List<string> issues)
    {
        // Перевірка на нульові значення батареї
        if (newState.BatteryPercentage == 0 && previousState.BatteryPercentage > 10)
        {
            issues.Add($"Battery percentage dropped to 0 from {previousState.BatteryPercentage}%");
            return false;
        }

        // Перевірка діапазону напруги батареї
        if (newState.BatteryVoltage < MIN_BATTERY_VOLTAGE || newState.BatteryVoltage > MAX_BATTERY_VOLTAGE)
        {
            issues.Add($"Battery voltage out of range: {newState.BatteryVoltage}V (valid: {MIN_BATTERY_VOLTAGE}-{MAX_BATTERY_VOLTAGE}V)");
            return false;
        }

        // Перевірка різких стрибків відсотка батареї
        var batteryPercentageChange = Math.Abs(newState.BatteryPercentage - previousState.BatteryPercentage);
        if (batteryPercentageChange > MAX_BATTERY_PERCENTAGE_CHANGE)
        {
            issues.Add($"Battery percentage changed too rapidly: {previousState.BatteryPercentage}% → {newState.BatteryPercentage}% (Δ{batteryPercentageChange}%)");
            return false;
        }

        // Перевірка різких стрибків напруги батареї
        var batteryVoltageChange = Math.Abs(newState.BatteryVoltage - previousState.BatteryVoltage);
        if (batteryVoltageChange > MAX_BATTERY_VOLTAGE_CHANGE)
        {
            issues.Add($"Battery voltage changed too rapidly: {previousState.BatteryVoltage}V → {newState.BatteryVoltage}V (Δ{batteryVoltageChange}V)");
            return false;
        }

        return true;
    }

    private bool IsValidPVData(InverterState newState, InverterState previousState, List<string> issues)
    {
        // PV напруга може бути 0 вночі, це нормально
        if (newState.PVVoltage < MIN_PV_VOLTAGE || newState.PVVoltage > MAX_PV_VOLTAGE)
        {
            issues.Add($"PV voltage out of range: {newState.PVVoltage}V (valid: {MIN_PV_VOLTAGE}-{MAX_PV_VOLTAGE}V)");
            return false;
        }

        // Від'ємна потужність PV - це аномалія
        if (newState.PVPower < 0)
        {
            issues.Add($"Negative PV power: {newState.PVPower}W");
            return false;
        }

        return true;
    }

    private bool IsValidGridData(InverterState newState, InverterState previousState, List<string> issues)
    {
        // Перевірка напруги мережі (якщо є підключення до мережі)
        if (newState.GridVoltage > 0)
        {
            if (newState.GridVoltage < MIN_GRID_VOLTAGE || newState.GridVoltage > MAX_GRID_VOLTAGE)
            {
                issues.Add($"Grid voltage out of range: {newState.GridVoltage}V (valid: {MIN_GRID_VOLTAGE}-{MAX_GRID_VOLTAGE}V)");
                return false;
            }

            // Перевірка частоти мережі
            if (newState.GridFrequency < MIN_FREQUENCY || newState.GridFrequency > MAX_FREQUENCY)
            {
                issues.Add($"Grid frequency out of range: {newState.GridFrequency}Hz (valid: {MIN_FREQUENCY}-{MAX_FREQUENCY}Hz)");
                return false;
            }
        }

        return true;
    }

    private bool IsValidPowerData(InverterState newState, InverterState previousState, List<string> issues)
    {
        // Перевірка різких стрибків потужності
        var outputPowerChange = Math.Abs(newState.OutputPower - previousState.OutputPower);
        if (outputPowerChange > MAX_POWER_CHANGE)
        {
            issues.Add($"Output power changed too rapidly: {previousState.OutputPower}W → {newState.OutputPower}W (Δ{outputPowerChange}W)");
            return false;
        }

        return true;
    }

    private bool IsValidTemperatureData(InverterState newState, InverterState previousState, List<string> issues)
    {
        // Перевірка діапазонів температури
        if (newState.DCDCTemperature < MIN_TEMPERATURE || newState.DCDCTemperature > MAX_TEMPERATURE)
        {
            issues.Add($"DCDC temperature out of range: {newState.DCDCTemperature}°C (valid: {MIN_TEMPERATURE}-{MAX_TEMPERATURE}°C)");
            return false;
        }

        if (newState.InverterTemperature < MIN_TEMPERATURE || newState.InverterTemperature > MAX_TEMPERATURE)
        {
            issues.Add($"Inverter temperature out of range: {newState.InverterTemperature}°C (valid: {MIN_TEMPERATURE}-{MAX_TEMPERATURE}°C)");
            return false;
        }

        // Перевірка різких стрибків температури
        var dcdcTempChange = Math.Abs(newState.DCDCTemperature - previousState.DCDCTemperature);
        if (dcdcTempChange > MAX_TEMPERATURE_CHANGE)
        {
            issues.Add($"DCDC temperature changed too rapidly: {previousState.DCDCTemperature}°C → {newState.DCDCTemperature}°C (Δ{dcdcTempChange}°C)");
            return false;
        }

        var inverterTempChange = Math.Abs(newState.InverterTemperature - previousState.InverterTemperature);
        if (inverterTempChange > MAX_TEMPERATURE_CHANGE)
        {
            issues.Add($"Inverter temperature changed too rapidly: {previousState.InverterTemperature}°C → {newState.InverterTemperature}°C (Δ{inverterTempChange}°C)");
            return false;
        }

        return true;
    }

    private bool LogAndReturn(bool result, List<string> issues, InverterState newState)
    {
        if (!result && issues.Any())
        {
            _logger?.LogWarning($"Invalid reading detected - using previous values. Issues: {string.Join("; ", issues)}");
            _logger?.LogDebug($"Rejected state: Battery={newState.BatteryPercentage}%, BatV={newState.BatteryVoltage}V, PV={newState.PVVoltage}V, Grid={newState.GridVoltage}V");
        }
        return result;
    }

    /// <summary>
    /// Створює копію даних з попереднього стану (для використання при відхиленні невалідних даних)
    /// </summary>
    public void CopyValidData(InverterState target, InverterState source)
    {
        // Копіюємо тільки нестабільні значення, які можуть мати викиди
        target.BatteryPercentage = source.BatteryPercentage;
        target.BatteryVoltage = source.BatteryVoltage;
        target.BatteryCurrent = source.BatteryCurrent;
        target.BatteryPower = source.BatteryPower;
        target.BatteryAverageCurrent = source.BatteryAverageCurrent;

        // Можна додати інші поля за потреби
    }
}