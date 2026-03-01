using System;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace LibreHwMonitor
{
    public static class DataRaw
    {
        private static Computer? _computer;

        public class HardwareInfo
        {
            public string Name { get; set; } = string.Empty;
            public HardwareType Type { get; set; }
            public List<SensorInfo> Sensors { get; set; } = new();
        }

        public class SensorInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public double? Value { get; set; }
        }

        public static List<HardwareInfo> CollectHardwareInfo()
        {
            var result = new List<HardwareInfo>();

            if (_computer == null)
            {
                _computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = false,   // 🔥 CỰC KỲ QUAN TRỌNG – TRÁNH NVML
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsNetworkEnabled = true,
                    IsStorageEnabled = true
                };

                _computer.Open(); // ✅ KHÔNG CRASH
            }

            _computer.Accept(new UpdateVisitor());

            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType is not (
                    HardwareType.Cpu or
                    HardwareType.Memory or
                    HardwareType.Storage))
                    continue;

                var hw = new HardwareInfo
                {
                    Name = hardware.Name ?? "Unknown",
                    Type = hardware.HardwareType
                };

                CollectSensorsRecursive(hardware, hw);
                result.Add(hw);
            }

            return result;
        }

        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer) => computer.Traverse(this);

            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (var sub in hardware.SubHardware)
                    sub.Accept(this);
            }

            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        private static void CollectSensorsRecursive(IHardware hardware, HardwareInfo info)
        {
            foreach (var sensor in hardware.Sensors)
            {
                // ✅ CPU TEMP – LẤY PACKAGE (net48-safe)
                if (hardware.HardwareType == HardwareType.Cpu &&
                    sensor.SensorType == SensorType.Temperature &&
                    sensor.Name != null &&
                    sensor.Name.IndexOf("Package", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                info.Sensors.Add(new SensorInfo
                {
                    Name = sensor.Name ?? "Unknown",
                    Type = sensor.SensorType.ToString(),
                    Value = sensor.Value
                });
            }

            foreach (var sub in hardware.SubHardware)
                CollectSensorsRecursive(sub, info);
        }
    }
}
