using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreHwMonitor
{
    public static class DataCleaner
    {
        public class HardwareNode
        {
            public string Name { get; set; } = string.Empty;
            public int Type { get; set; }
            public List<SensorNode> Sensors { get; set; } = new List<SensorNode>();
        }

        public class SensorNode
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public double Value { get; set; }
        }

        private static readonly Dictionary<int, Func<SensorNode, bool>> SensorFilters =
            new Dictionary<int, Func<SensorNode, bool>>
        {
            // CPU
            [2] = s =>
                (s.Type == "Temperature" && (
                    s.Name == "Core Max" ||
                    s.Name == "Core Average" ||
                    (s.Name?.StartsWith("CPU Core #1") ?? false) ||
                    s.Name == "CPU Package"
                )) ||
                (s.Type == "Power" && (
                    s.Name == "CPU Package" ||
                    s.Name == "CPU Cores" ||
                    s.Name == "CPU Memory"
                )) ||
                (s.Type == "Voltage" && s.Name == "CPU Core") ||
                (s.Type == "Clock" && s.Name == "Bus Speed") ||
                (s.Type == "Load" && s.Name == "CPU Total"),

            // RAM
            [3] = s =>
                s.Name == "Memory Used" ||
                s.Name == "Memory Available" ||
                (s.Type == "Load" && s.Name == "Memory"),

            // GPU
            [4] = s =>
                (s.Name == "GPU Core" && s.Type == "Temperature") ||
                s.Name == "GPU Fan" ||
                (s.Name?.StartsWith("GPU Memory") ?? false),

            // Disk
            [7] = s =>
                s.Name == "Temperature" ||
                s.Name == "Data Read" ||
                s.Name == "Data Written"
        };

        public static List<HardwareNode> CleanHardware(List<HardwareNode> hardwareList)
        {
            var result = new List<HardwareNode>();

            foreach (var hardware in hardwareList)
            {
                if (hardware == null) continue;

                var cleanHardware = new HardwareNode
                {
                    Name = hardware.Name,
                    Type = hardware.Type
                };

                // Clean and add sensors
                foreach (var sensor in hardware.Sensors.Where(s => s != null))
                {
                    // Skip sensors with invalid values
                    if (double.IsNaN(sensor.Value) || double.IsInfinity(sensor.Value))
                        continue;

                    if (SensorFilters.TryGetValue(hardware.Type, out var filter) && filter(sensor))
                    {
                        cleanHardware.Sensors.Add(new SensorNode
                        {
                            Name = CleanSensorName(sensor.Name),
                            Type = sensor.Type,
                            Value = Math.Round(sensor.Value, 2) // Round to 2 decimal places
                        });
                    }
                }

                // Only add hardware if it has sensors
                if (cleanHardware.Sensors.Count > 0)
                {
                    result.Add(cleanHardware);
                }
            }

            return result;
        }

        private static string CleanSensorName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Unknown";

            // Remove common prefixes and clean up the name
            return name
                .Replace(" ", "")
                .Replace("#", "")
                .Replace("/", "_")
                .Replace("\\", "_");
        }
    }
}
