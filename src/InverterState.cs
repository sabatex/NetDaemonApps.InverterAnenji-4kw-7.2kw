using NetDaemon.Extensions.MqttEntityManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InverterAnenji;

public class InverterState
{
    public WorkingMode WorkingMode { get; set; }  //201    
    public float GridVoltage { get; set; }        //202
    public float GridFrequency { get; set; }      //203
    public short GridLoadPower { get; set; }     //204


    public float InverterVoltage { get; set; }    //205
    public float InverterCurrent { get; set; }    //206
    public float InverterFrequency { get; set; }  //207 
    public short InverterPower { get; set; }     //208
    public short InverterChargingPower { get; set; }     //209


    public float OutputVoltage { get; set; }        //202
    public float OutputCurrent { get; set; }
    public float OutputFrequency { get; set; }
    public short OutputPower { get; set; }
    public short OutputApparentPower { get; set; }


    public float BatteryVoltage { get; set; }
    public float BatteryCurrent { get; set; }
    public short BatteryPower { get; set; }


    public float PVVoltage { get; set; }    //205
    public float PVCurrent { get; set; }    //206
    public short PVPower { get; set; }     //208
    public short PVChargingPower { get; set; }     //209


    public short LoadPercentage { get; set; }
    public short DCDCTemperature { get; set; }
    public short InverterTemperature { get; set; }
    public short BatteryPercentage { get; set; }

    public float BatteryAverageCurrent { get; set; }
    public float InverterChargingAverageCurrent { get; set; }
    public float PVChargingAverageCurrent { get; set; }


     object stateTopic = "homeassistant/sensor/inverter_anenji4000/state";
     object device = new { identifiers = new[] { "inverter_anenji4000" }, name = "OffGrid Inverter Anenji", model = "anj-4kW-24V", manufacturer = "Anennji" };

    public InverterState()
    {
        
    }


    public async Task CreateSensors(IMqttEntityManager entityManager)
    {
        await entityManager.CreateAsync("sensor.inverter_anenji4000_working_mode", new EntityCreationOptions
        {
            Name = "Working Mode",
        },
        new
        {
            icon = "mdi:list-status",
            state_topic = stateTopic,
            value_template = "{{ value_json.mode }}",
            device
        });
        #region GRID
        await entityManager.CreateAsync("sensor.inverter_anenji4000_grid_voltage", new EntityCreationOptions
        {
            Name = "Grid voltage",
            DeviceClass = "VOLTAGE"
        }, new 
        {
            unit_of_measurement = "V",
            state_topic = stateTopic,
            value_template = "{{ value_json.grid_voltage }}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_grid_frequency", new EntityCreationOptions
        {
            Name = "Grid Frequency",
            DeviceClass = "FREQUENCY"
        },new 
        {
            unit_of_measurement = "Hz",
            state_topic = stateTopic,
            value_template = "{{ value_json.grid_frequency }}",
            device
        });
        await entityManager.CreateAsync("sensor.anenji4000_grid_load_power", new EntityCreationOptions
        {
            Name = "Grid Load power",
            DeviceClass = "Power"
        }, new 
        {
            unit_of_measurement = "w",
            state_topic = stateTopic,
            value_template = "{{ value_json.grid_power }}",
            device

        });
        #endregion
        #region Inverter
        await entityManager.CreateAsync("sensor.inverter_anenji4000_inverter_voltage", new EntityCreationOptions
        {
            Name = "Inverter voltage",
            DeviceClass = "VOLTAGE"
        }, new
        {
            unit_of_measurement = "V",
            state_topic = stateTopic,
            value_template = "{{ value_json.inverter_voltage }}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_inverter_current", new EntityCreationOptions
        {
            Name = "Inverter current",
            DeviceClass = "CURRENT"
        }, new
        {
            unit_of_measurement = "A",
            state_topic = stateTopic,
            value_template = "{{ value_json.inverter_current }}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_inverter_frequency", new EntityCreationOptions
        {
            Name = "Inverter Frequency",
            DeviceClass = "FREQUENCY"
        }, new
        {
            unit_of_measurement = "Hz",
            state_topic = stateTopic,
            value_template = "{{ value_json.inverter_frequency }}",
            device
        });
        await entityManager.CreateAsync("sensor.anenji4000_inverter_power", new EntityCreationOptions
        {
            Name = "Inverter Load power",
            DeviceClass = "Power"
        }, new
        {
            unit_of_measurement = "w",
            state_topic = stateTopic,
            value_template = "{{ value_json.inverter_power }}",
            device

        });
        await entityManager.CreateAsync("sensor.anenji4000_inverter_charging_power", new EntityCreationOptions
        {
            Name = "Inverter charging power",
            DeviceClass = "Power"
        }, new
        {
            unit_of_measurement = "w",
            state_topic = stateTopic,
            value_template = "{{ value_json.inverter_charging_power }}",
            device

        });
        #endregion
        #region Output
        await entityManager.CreateAsync("sensor.inverter_anenji4000_output_voltage", new EntityCreationOptions
        {
            Name = "Output voltage",
            DeviceClass = "VOLTAGE"
        }, new
        {
            unit_of_measurement = "V",
            state_topic = stateTopic,
            value_template = "{{ value_json.output_voltage }}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_output_current", new EntityCreationOptions
        {
            Name = "Output current",
            DeviceClass = "VOLTAGE"
        }, new
        {
            unit_of_measurement = "A",
            state_topic = stateTopic,
            value_template = "{{ value_json.output_current }}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_output_frequency", new EntityCreationOptions
        {
            Name = "Output Frequency",
            DeviceClass = "FREQUENCY"
        }, new
        {
            unit_of_measurement = "Hz",
            state_topic = stateTopic,
            value_template = "{{ value_json.output_frequency }}",
            device
        });
        await entityManager.CreateAsync("sensor.anenji4000_output_power", new EntityCreationOptions
        {
            Name = "Output power",
            DeviceClass = "Power"
        }, new
        {
            unit_of_measurement = "w",
            state_topic = stateTopic,
            value_template = "{{ value_json.output_power }}",
            device

        });
        await entityManager.CreateAsync("sensor.anenji4000_output_apparent_power", new EntityCreationOptions
        {
            Name = "Output apparent power",
            DeviceClass = "APPARENT_POWER"
        }, new
        {
            unit_of_measurement = "VA",
            state_topic = stateTopic,
            value_template = "{{ value_json.output_apparent_power }}",
            device

        });
        #endregion

        #region Batery
        await entityManager.CreateAsync("sensor.inverter_anenji4000_batery_voltage", new EntityCreationOptions
        {
            Name = "Batery voltage",
            DeviceClass = "VOLTAGE"
        }, new
        {
            unit_of_measurement = "V",
            state_topic = stateTopic,
            value_template = "{{ value_json.batery_voltage}}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_batery_current", new EntityCreationOptions
        {
            Name = "Batery current",
            DeviceClass = "CURRENT"
        }, new
        {
            unit_of_measurement = "A",
            state_topic = stateTopic,
            value_template = "{{ value_json.batery_current }}",
            device
        });
        await entityManager.CreateAsync("sensor.anenji4000_batery_power", new EntityCreationOptions
        {
            Name = "Batery power",
            DeviceClass = "Power"
        }, new
        {
            unit_of_measurement = "w",
            state_topic = stateTopic,
            value_template = "{{ value_json.batery_power }}",
            device

        });

        #endregion

        #region PV
        await entityManager.CreateAsync("sensor.inverter_anenji4000_pv_voltage", new EntityCreationOptions
        {
            Name = "PV voltage",
            DeviceClass = "VOLTAGE"
        }, new
        {
            unit_of_measurement = "V",
            state_topic = stateTopic,
            value_template = "{{ value_json.pv_voltage}}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_pv_current", new EntityCreationOptions
        {
            Name = "PV current",
            DeviceClass = "VOLTAGE"
        }, new
        {
            unit_of_measurement = "A",
            state_topic = stateTopic,
            value_template = "{{ value_json.pv_current }}",
            device
        });
        await entityManager.CreateAsync("sensor.anenji4000_pv_power", new EntityCreationOptions
        {
            Name = "PV power",
            DeviceClass = "Power"
        }, new
        {
            unit_of_measurement = "w",
            state_topic = stateTopic,
            value_template = "{{ value_json.pv_power }}",
            device

        });
        await entityManager.CreateAsync("sensor.anenji4000_pv_charging_power", new EntityCreationOptions
        {
            Name = "PV charging power",
            DeviceClass = "Power"
        }, new
        {
            unit_of_measurement = "w",
            state_topic = stateTopic,
            value_template = "{{ value_json.pv_charging_power }}",
            device

        });
        #endregion


        await entityManager.CreateAsync("sensor.anenji4000_load_percentage", new EntityCreationOptions
        {
            Name = "Load percentage",
            DeviceClass = "Percentage"
        }, new
        {
            unit_of_measurement = "%",
            state_topic = stateTopic,
            value_template = "{{ value_json.load_percentage}}",
            device

        });
        await entityManager.CreateAsync("sensor.anenji4000_dcdc_temperature", new EntityCreationOptions
        {
            Name = "DCDC Temperature",
            DeviceClass = "Temperature"
        }, new
        {
            unit_of_measurement = "C",
            state_topic = stateTopic,
            value_template = "{{ value_json.dcdc_temperature}}",
            device

        });
        await entityManager.CreateAsync("sensor.anenji4000_inverter_temperature", new EntityCreationOptions
        {
            Name = "Inverter Temperature",
            DeviceClass = "Temperature"
        }, new
        {
            unit_of_measurement = "C",
            state_topic = stateTopic,
            value_template = "{{ value_json.inverter_temperature}}",
            device

        });
        await entityManager.CreateAsync("sensor.anenji4000_batery_percentage", new EntityCreationOptions
        {
            Name = "Batery percentage",
            DeviceClass = "BATTERY"
        }, new
        {
            unit_of_measurement = "%",
            state_topic = stateTopic,
            value_template = "{{ value_json.batery_percentage}}",
            device

        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_batery_average_current", new EntityCreationOptions
        {
            Name = "Batery average current",
            DeviceClass = "CURRENT"
        }, new
        {
            unit_of_measurement = "A",
            state_topic = stateTopic,
            value_template = "{{ value_json.batery_average_current }}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_inverter_charging_current", new EntityCreationOptions
        {
            Name = "Inverter charging average current",
            DeviceClass = "CURRENT"
        }, new
        {
            unit_of_measurement = "A",
            state_topic = stateTopic,
            value_template = "{{ value_json.inverter_charging_current }}",
            device
        });
        await entityManager.CreateAsync("sensor.inverter_anenji4000_pv_charging_current", new EntityCreationOptions
        {
            Name = "PV charging average current",
            DeviceClass = "CURRENT"
        }, new
        {
            unit_of_measurement = "A",
            state_topic = stateTopic,
            value_template = "{{ value_json.pv_charging_current}}",
            device
        });

    }


    public async Task SendMQTT(IMqttEntityManager entityManager)
    {
        var newState = new
        {
            grid_voltage = GridVoltage,
            mode = WorkingMode.ToString(),
            grid_frequency = GridFrequency,
            grid_power = GridLoadPower,
            inverter_voltage = InverterVoltage,
            inverter_current = InverterCurrent,
            inverter_frequency = InverterFrequency,
            inverter_power = InverterPower,
            inverter_charging_power = InverterChargingPower,
            output_voltage = OutputVoltage,
            output_current = OutputCurrent,
            output_frequency = OutputFrequency,
            output_power = OutputPower,
            output_apparent_power = OutputApparentPower,
            batery_voltage = BatteryVoltage,
            batery_current = BatteryCurrent,
            batery_power = BatteryPower,
            pv_voltage = PVVoltage,
            pv_current = PVCurrent,
            pv_power = PVPower,
            pv_charging_power=PVChargingPower,
            load_percentage = LoadPercentage,
            dcdc_temperature = DCDCTemperature,
            inverter_temperature = InverterTemperature,
            batery_percentage = BatteryPercentage,
            batery_average_current = BatteryAverageCurrent,
            inverter_charging_current = InverterChargingAverageCurrent,
            pv_charging_current=PVChargingAverageCurrent


        };


        await entityManager.SetStateAsync("sensor.inverter_anenji4000", JsonSerializer.Serialize(newState));

    }



}
