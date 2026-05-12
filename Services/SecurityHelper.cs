using System;
using System.Security.Cryptography;
using System.Text;

namespace ArcelikApp.Services
{
    public static class SecurityHelper
    {
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        public static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static string GetDeviceId()
        {
            try
            {
                var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach (var nic in nics)
                {
                    if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                        !string.IsNullOrEmpty(nic.GetPhysicalAddress().ToString()))
                    {
                        return nic.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch { }
            return Environment.MachineName; // Fallback
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        public class HardwareProfile
        {
            public string CpuId { get; set; } = string.Empty;
            public string MotherboardId { get; set; } = string.Empty;
            public string DiskId { get; set; } = string.Empty;
        }

        public static async System.Threading.Tasks.Task<HardwareProfile> GetHardwareProfileAsync()
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                return new HardwareProfile
                {
                    CpuId = GetWmiProperty("Win32_Processor", "ProcessorId"),
                    MotherboardId = GetWmiProperty("Win32_ComputerSystemProduct", "UUID"),
                    DiskId = GetWmiProperty("Win32_DiskDrive", "SerialNumber")
                };
            });
        }

        private static string GetWmiProperty(string wmiClass, string property)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var val = obj[property]?.ToString()?.Trim();
                    // Some motherboards return generic strings, ignore them
                    if (!string.IsNullOrEmpty(val) && !val.Contains("FFFFFFFF"))
                    {
                        return val;
                    }
                }
            }
            catch { }
            return "UNKNOWN";
        }
    }
}
