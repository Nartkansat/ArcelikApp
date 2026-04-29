using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcelikExcelApp.Views;
using ArcelikApp.Services;

namespace ArcelikExcelApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string WampProcessName = "wampmanager";
    private const string WampExePath     = @"C:\wamp64\wampmanager.exe";

    protected override async void OnStartup(StartupEventArgs e)
    {


        base.OnStartup(e);

        // Güncelleme kontrolü (arka planda)
        _ = Task.Run(() =>
        {
            try { UpdateService.CheckForUpdates(); } catch { }
        });

        // WAMP kontrolünü arka planda başlat (UI'ı bloke etme)
        _ = Task.Run(() => EnsureWampRunningAsync());

        // Auto-login kontrolünü arka planda yap
        bool autoLoginSuccess = false;
        try
        {
            autoLoginSuccess = await Task.Run(() => ArcelikApp.Services.AuthService.CheckAutoLogin());
        }
        catch { }

        if (autoLoginSuccess)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        else
        {
            var loginWindow = new LoginWindow();
            // LoginWindow zaten Loaded olayında bağlantıyı tekrar kontrol edecek
            loginWindow.Show();
        }
    }

    private static async Task EnsureWampRunningAsync()
    {
        try
        {
            // Zaten çalışıyor mu?
            bool calisiyorMu = Process.GetProcesses()
                .Any(p => p.ProcessName.Equals(WampProcessName, StringComparison.OrdinalIgnoreCase));

            if (calisiyorMu)
            {
                Debug.WriteLine($"{WampProcessName} zaten çalışıyor.");
                return;
            }

            // Kurulu mu?
            if (!File.Exists(WampExePath))
            {
                Debug.WriteLine($"WampServer bulunamadı: {WampExePath}");
                return;
            }

            // Başlat (yönetici olarak)
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName        = WampExePath,
                    UseShellExecute = true,
                    Verb            = "runas"
                };
                Process.Start(startInfo);
                Debug.WriteLine($"{WampProcessName} başlatıldı.");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Debug.WriteLine($"WampManager başlatılamadı: {ex.Message}");
                return;
            }

            // WAMP process görünene kadar max 10 sn bekle (async)
            await WaitForProcessAsync(WampProcessName, timeoutSeconds: 10);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EnsureWampRunning hatası: {ex.Message}");
        }
    }

    private static async Task WaitForProcessAsync(string processName, int timeoutSeconds)
    {
        int elapsed = 0;
        const int interval = 1000; // ms
        int maxMs = timeoutSeconds * 1000;

        while (elapsed < maxMs)
        {
            bool running = Process.GetProcesses()
                .Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

            if (running) return;

            await Task.Delay(interval);
            elapsed += interval;
        }

        Debug.WriteLine($"{processName} {timeoutSeconds} saniye içinde başlamadı, devam ediliyor.");
    }
}
