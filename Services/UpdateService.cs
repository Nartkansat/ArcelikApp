using System;
using System.Collections.Generic;
using System.Net.Http;
using AutoUpdaterDotNET;

namespace ArcelikApp.Services
{
    public static class UpdateService
    {
        // !!! ÖNEMLİ !!!: GitHub'dan aldığınız Personal Access Token'ı buraya yazın.
        private const string GitHubToken = "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"; 
        
        // !!! ÖNEMLİ !!!: Kendi kullanıcı adınız ve repo adınızla güncelleyin.
        private const string UpdateXmlUrl = "https://raw.githubusercontent.com/nartkansat/ArcelikApp/main/update.xml";

        public static void CheckForUpdates()
        {
            if (string.IsNullOrEmpty(GitHubToken) || GitHubToken == "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")
            {
                // Token ayarlanmamışsa güncelleme kontrolü yapma (Hata vermemesi için)
                return;
            }

            // Private Repo Erişimi için GitHub Token'ı kullan
            // CustomAuthentication, Authorization header'ının değerini doğrudan alır.
            var auth = new CustomAuthentication($"token {GitHubToken}");
            AutoUpdater.BasicAuthXML = auth;
            AutoUpdater.BasicAuthDownload = auth;

            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            
            // Güncelleme kontrolünü başlat
            AutoUpdater.Start(UpdateXmlUrl);
        }
    }
}
