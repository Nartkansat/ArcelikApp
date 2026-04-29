using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using ArcelikExcelApp.Views;

namespace ArcelikExcelApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string WampProcessName = "wampmanager";
    private const string WampExePath     = @"C:\wamp64\wampmanager.exe";

    protected override void OnStartup(StartupEventArgs e)
    {
        // Güncelleme kontrolü
        ArcelikApp.Services.UpdateService.CheckForUpdates();

        EnsureWampRunning();
        base.OnStartup(e);

        bool autoLoginSuccess = false;
        try
        {
            // Veritabanı henüz hazır olmayabilir, bu yüzden AuthService.CheckAutoLogin() içinde 
            // bağlantı hatası alma riskine karşı try-catch kullanıyoruz.
            autoLoginSuccess = ArcelikApp.Services.AuthService.CheckAutoLogin();
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

    private static void EnsureWampRunning()
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
            MessageBox.Show(
                $"WampServer bulunamadı:\n{WampExePath}\n\nUygulama yine de başlatılıyor.",
                "WampServer Bulunamadı",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
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
            // Kullanıcı UAC'ı reddetti veya başka hata
            MessageBox.Show(
                $"WampManager başlatılamadı:\n{ex.Message}\n\nUygulama yine de başlatılıyor.",
                "Başlatma Hatası",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // WAMP process görünene kadar max 15 sn bekle
        WaitForProcess(WampProcessName, timeoutSeconds: 15);
    }

    private static void WaitForProcess(string processName, int timeoutSeconds)
    {
        int elapsed = 0;
        const int interval = 500; // ms
        int maxMs = timeoutSeconds * 1000;

        while (elapsed < maxMs)
        {
            bool running = Process.GetProcesses()
                .Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

            if (running) return;

            Thread.Sleep(interval);
            elapsed += interval;
        }

        Debug.WriteLine($"{processName} {timeoutSeconds} saniye içinde başlamadı, devam ediliyor.");
    }
}
