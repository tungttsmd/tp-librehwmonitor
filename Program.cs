using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LibreHwMonitor
{
    class Program
    {
        private static MetricWebserver? _metricWebserver;
        private static readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Env.Load();

            int interval = Env.GetInt("INTERVAL_MS", 12000);
            string ip = Env.Get("BIND_IP", "127.0.0.1");
            int port = Env.GetInt("PORT", 1883);

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Đang tắt dịch vụ...");
                _shutdownEvent.Set();
                e.Cancel = true;
            };

            try
            {
                _metricWebserver = new MetricWebserver(ip, port, interval);
                _metricWebserver.Start();
            
                Console.WriteLine($"=============================================================================\n");
                Console.WriteLine($"[INFO] Cấu hình dịch vụ .env: interval={interval}ms ip={ip} port={port}");
                Console.WriteLine($"=============================================================================\n");
                Console.WriteLine($"[INFO] Nhấn Ctrl+C để dừng dịch vụ...");
                Console.WriteLine($"\n");

                _shutdownEvent.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Lỗi: {ex.Message}");
            }
            finally
            {
                _metricWebserver?.Stop();
                _metricWebserver?.Dispose();
            }
        }
    }

    public class CamelCaseNamingStrategy : NamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
