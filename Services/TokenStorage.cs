using System;
using System.IO;

namespace ArcelikApp.Services
{
    public static class TokenStorage
    {
        private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth.token");

        public static void SaveToken(string token)
        {
            try { File.WriteAllText(FilePath, token); } catch { }
        }

        public static string? GetToken()
        {
            try { return File.Exists(FilePath) ? File.ReadAllText(FilePath) : null; } catch { return null; }
        }

        public static void ClearToken()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
