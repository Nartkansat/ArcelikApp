using System;
using System.Collections.Generic;
using System.Net.Http;
using AutoUpdaterDotNET;

namespace ArcelikApp.Services
{
    public static class UpdateService
    {
        // GitHub deponuz artık Public olduğu için Token'a gerek yoktur.
        private const string UpdateXmlUrl = "https://raw.githubusercontent.com/nartkansat/ArcelikApp/main/update.xml";

        public static void CheckForUpdates()
        {
            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            
            // Güncelleme kontrolünü başlat
            try 
            {
                AutoUpdater.Start(UpdateXmlUrl);
            }
            catch (Exception ex)
            {
                NotificationService.ShowToast(
                    "Güncelleme Hatası", 
                    "Güncelleme sunucusuna bağlanırken bir sorun oluştu.", 
                    "Error");
            }
        }
    }
}
