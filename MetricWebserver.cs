using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using LibreHardwareMonitor.Hardware;
using System.Linq;

namespace LibreHwMonitor
{
    public class MetricWebserver : IDisposable
    {
        private readonly HttpListener _listener;

        private readonly string _ip;         
        private readonly int _port;
        private volatile bool _isRunning;
        private readonly object _dataLock = new object();
        private string _cachedJsonMetrics = "{}";
        private string _cachedRawMetrics = "{}";
        private readonly int _updateIntervalMs;
        private Thread? _updateThread;

        public MetricWebserver(string ip, int port, int updateIntervalMs)
        {
            _ip = ip;
            _port = port;
            _updateIntervalMs = updateIntervalMs;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{_ip}:{_port}/".ToLower());
        }

        public void Start()
        {
            if (_isRunning) return;
        
            _isRunning = true;
            _listener.Start();
        
            // Start the update thread
            _updateThread = new Thread(UpdateDataLoop)
            {
                IsBackground = true
            };
            _updateThread.Start();
        
            // Start the HTTP listener loop
            Task.Run(ListenForRequests);
            Console.WriteLine($"\n");
            Console.WriteLine($"[INFO] LibreHWMonitor >> Đã khởi động webserver tại http://{_ip}:{_port}/");
            Console.WriteLine($"[INFO] LibreHWMonitor >> Dữ liệu metrics (thực đo): http://{_ip}:{_port}/metrics");
            Console.WriteLine($"[INFO] LibreHWMonitor >> Dữ liệu raw (toàn bộ): http://{_ip}:{_port}/raw");

            Console.WriteLine($"\n");
            Console.WriteLine($"[INFO] LibreHWMonitor >> VUI LÒNG CHẠY DƯỚI QUYỀN ADMINISTRATION ĐỂ LẤY ĐỦ CẢM BIẾN PHẦN CỨNG!");
            Console.WriteLine($"\n");
      
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            _updateThread?.Join();
        }

        private async void ListenForRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    Console.WriteLine($"[INFO] >> Data requested by some process.");
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xử lý request: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                string responseString;
                string contentType = "application/json";

                if (request.Url == null)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    responseString = "{\"error\":\"Invalid request URL\"}";
                }
                else if (request.Url.AbsolutePath == "/metrics")
                {
                    responseString = GetCachedMetrics();
                }
                else if (request.Url.AbsolutePath == "/raw")
                {
                    responseString = GetCachedRawMetrics();
                }
                else if (request.Url.AbsolutePath == "/")
                {
                    responseString = "<html><body><h1>LibreHwMonitor Metrics Server</h1>" +
                                    "<p>Dịch vụ có sẵn:</p>" +
                                    "<ul>" +
                                    "<li><a href='/metrics'>/metrics</a> - Dữ liệu metrics đã được xử lý</li>" +
                                    "<li><a href='/raw'>/raw</a> - Dữ liệu hardware raw</li>" +
                                    "</ul></body></html>";
                    contentType = "text/html";
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    responseString = "{\"error\":\"Not found\"}";
                }

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = $"{contentType}; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xử lý request: {ex.Message}");
            }
        }

        private string GetCachedMetrics()
        {
            lock (_dataLock)
            {
                return _cachedJsonMetrics;
            }
        }

        private string GetCachedRawMetrics()
        {
            lock (_dataLock)
            {
                return _cachedRawMetrics;
            }
        }

        private void UpdateDataLoop()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            while (_isRunning)
            {
                try
                {
                    // Collect raw data
                    var hardwareData = DataRaw.CollectHardwareInfo();
                    var dict = hardwareData
                        .GroupBy(h => h.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => g.First()
                        );
                    // Cache raw data
                    var rawJson = JsonConvert.SerializeObject(dict, jsonSettings);
                
                    // Convert and clean data
                    var hardwareNodes = ConvertToHardwareNodes(dict);
                    var cleanedData = DataCleaner.CleanHardware(hardwareNodes);
                    var cleanedJson = JsonConvert.SerializeObject(cleanedData, jsonSettings);

                    // Update cached data
                    lock (_dataLock)
                    {
                        _cachedRawMetrics = rawJson;
                        _cachedJsonMetrics = cleanedJson;
                    }
                
                    Console.WriteLine($"[INFO] LibreHWMonitor >> Sensor webserver refreshed...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] LibreHWMonitor >> Error updating metrics: {ex.Message}");
                }

                Thread.Sleep(_updateIntervalMs);
            }
        }

        private List<DataCleaner.HardwareNode> ConvertToHardwareNodes(Dictionary<string, DataRaw.HardwareInfo> hardwareData)
        {
            var result = new List<DataCleaner.HardwareNode>();

            foreach (var kv in hardwareData)
            {
                var hw = kv.Value;

                if (hw == null) continue;

                var node = new DataCleaner.HardwareNode
                {
                    Name = hw.Name ?? kv.Key,
                    Type = (int)hw.Type
                };

                if (hw.Sensors != null)
                {
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor?.Value.HasValue == true)
                        {
                            node.Sensors.Add(new DataCleaner.SensorNode
                            {
                                Name = sensor.Name ?? "Unknown",
                                Type = sensor.Type ?? "Unknown",
                                Value = sensor.Value.Value
                            });
                        }
                    }
                }

                if (node.Sensors.Count > 0)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        public void Dispose()
        {
            Stop();
            _listener.Close();
            GC.SuppressFinalize(this);
        }
    }
}
