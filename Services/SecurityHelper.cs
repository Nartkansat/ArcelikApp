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
    }
}
