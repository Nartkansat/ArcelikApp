using System;
using AutoUpdaterDotNET;

namespace ArcelikApp.Services
{
    public static class UpdateService
    {
        // GitHub deponuz Public olduğu için direkt raw URL kullanıyoruz.
        private const string UpdateXmlUrl = "https://raw.githubusercontent.com/nartkansat/ArcelikApp/main/update.xml";

        public static void CheckForUpdates()
        {
            AutoUpdater.ShowRemindLaterButton = false; // Hatırlat butonunu kaldır
            AutoUpdater.ShowSkipButton = false;        // Atla butonunu kaldır
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.Mandatory = true;               // Zorunlu güncelleme modu
            AutoUpdater.UpdateMode = Mode.Forced;       // Güncelleme penceresi kapanınca uygulamayı da kapatır
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            
            try 
            {
                AutoUpdater.Start(UpdateXmlUrl);
            }
            catch (Exception)
            {
                // Hata durumunda (internet yoksa vb.) loglanabilir
            }
        }
    }
}

