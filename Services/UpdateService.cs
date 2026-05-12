using System;
using System.Threading.Tasks;
using AutoUpdaterDotNET;

namespace ArcelikApp.Services
{
    public static class UpdateService
    {
        // GitHub deponuz Public olduğu için direkt raw URL kullanıyoruz.
        private const string UpdateXmlUrl = "https://raw.githubusercontent.com/nartkansat/ArcelikApp/main/update.xml";

        /// <summary>
        /// Güncelleme kontrolü yapar ve sonucu bekler.
        /// Güncelleme mevcutsa <c>true</c> döner (AutoUpdater zorlu dialog'u gösterir).
        /// İnternet yoksa veya güncelleme yoksa <c>false</c> döner.
        /// </summary>
        public static Task<bool> CheckForUpdatesAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            // Local function kullanıyoruz — delegate tipini açıkça yazmaktan kaçınmak için
            // Bu sayede event'ten unsubscribe da yapılabiliyor
            void Handler(UpdateInfoEventArgs args)
            {
                AutoUpdater.CheckForUpdateEvent -= Handler; // Sadece bir kez çalış

                if (args.Error != null || !args.IsUpdateAvailable)
                {
                    // İnternet yok veya güncelleme yok → uygulamayı engelleme
                    tcs.TrySetResult(false);
                }
                else
                {
                    // Güncelleme var → AutoUpdater zaten Forced dialog'u gösterecek
                    tcs.TrySetResult(true);
                }
            }

            AutoUpdater.CheckForUpdateEvent += Handler;
            AutoUpdater.ShowRemindLaterButton = false; // Hatırlat butonunu kaldır
            AutoUpdater.ShowSkipButton        = false; // Atla butonunu kaldır
            AutoUpdater.RunUpdateAsAdmin      = true;
            AutoUpdater.Mandatory             = true;              // Zorunlu güncelleme modu
            AutoUpdater.UpdateMode            = Mode.Forced;       // Dialog kapanınca uygulamayı da kapatır
            AutoUpdater.DownloadPath          = Environment.CurrentDirectory;

            try
            {
                AutoUpdater.Start(UpdateXmlUrl);
            }
            catch
            {
                // Başlatma hatası → engelleme
                AutoUpdater.CheckForUpdateEvent -= Handler;
                tcs.TrySetResult(false);
            }

            return tcs.Task;
        }
    }
}

