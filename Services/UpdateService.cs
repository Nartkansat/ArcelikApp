using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
using System.Windows;

namespace ArcelikApp.Services
{
    public static class UpdateService
    {
        // GitHub repository URL
        private const string GithubRepoUrl = "https://github.com/nartkansat/ArcelikApp";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                // Velopack UpdateManager oluştur
                var mgr = new UpdateManager(new GithubSource(GithubRepoUrl, null, false));

                // Güncelleme kontrolü yap
                var newVersion = await mgr.CheckForUpdatesAsync();
                if (newVersion == null)
                {
                    return; // Güncelleme yok
                }

                // Güncelleme bulundu, indir
                await mgr.DownloadUpdatesAsync(newVersion);

                // Kullanıcıya sor
                var result = MessageBox.Show(
                    $"Yeni bir sürüm mevcut ({newVersion.TargetFullRelease.Version}). Şimdi yükleyip yeniden başlatmak ister misiniz?",
                    "Güncelleme Hazır",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    // Güncellemeyi uygula ve yeniden başlat
                    mgr.ApplyUpdatesAndRestart(newVersion);
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda sessizce logla veya kullanıcıya bildir (opsiyonel)
                System.Diagnostics.Debug.WriteLine($"Velopack Hatası: {ex.Message}");
            }
        }
    }
}
