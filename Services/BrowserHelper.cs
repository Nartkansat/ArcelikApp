using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ArcelikApp.Services;

namespace ArcelikApp.Services
{
    public static class BrowserHelper
    {
        public static async Task OpenUrlAsync(string url)
        {
            try
            {
                // 1. Varsayılan tarayıcıyı dene
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    // 2. Chrome dene
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "chrome.exe",
                        Arguments = url,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    try
                    {
                        // 3. Edge dene
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "msedge.exe",
                            Arguments = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception)
                    {
                        // 4. Hiçbiri olmazsa hata mesajı
                        await ModernDialogService.ShowAsync("Tarayıcı Hatası", 
                            "Sisteminizde uygun bir internet tarayıcısı (Chrome, Edge vb.) bulunamadı veya başlatılamadı.", 
                            ModernDialogType.Error);
                    }
                }
            }
        }
    }
}
