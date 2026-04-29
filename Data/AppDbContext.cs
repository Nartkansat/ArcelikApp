using ArcelikApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ArcelikApp.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<KeaProduct> KeaProducts { get; set; }
        public DbSet<WhiteGoodsProduct> WhiteGoodsProducts { get; set; }
        public DbSet<OlizCampaign> OlizCampaigns { get; set; }
        public DbSet<CostCalculation> CostCalculations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        /// <summary>
        /// Son bağlantı durumunu tutar. True = bağlı, False = bağlantı yok.
        /// </summary>
        public static bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Son bağlantı hata mesajını tutar.
        /// </summary>
        public static string LastConnectionError { get; private set; } = string.Empty;

        public AppDbContext()
        {
        }

        public static bool TestConnection()
        {
            try
            {
                using var db = new AppDbContext();
                bool result = db.Database.CanConnect();
                IsConnected = result;
                if (result) LastConnectionError = string.Empty;
                return result;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastConnectionError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Bağlantıyı retry mekanizması ile test eder.
        /// maxRetries kadar dener, her denemede bekleme süresini artırır (exponential backoff).
        /// </summary>
        public static async Task<bool> TestConnectionWithRetryAsync(int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                Debug.WriteLine($"Veritabanı bağlantı denemesi {attempt}/{maxRetries}...");

                bool connected = await Task.Run(() => TestConnection());
                if (connected)
                {
                    Debug.WriteLine("Veritabanı bağlantısı başarılı.");
                    return true;
                }

                if (attempt < maxRetries)
                {
                    // Exponential backoff: 2s, 4s, 8s...
                    int delayMs = (int)Math.Pow(2, attempt) * 1000;
                    Debug.WriteLine($"Bağlantı başarısız, {delayMs / 1000} saniye sonra tekrar denenecek...");
                    await Task.Delay(delayMs);
                }
            }

            Debug.WriteLine("Tüm bağlantı denemeleri başarısız oldu.");
            return false;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                // Anabilgisayar ip adresi, db ye oradan baglaniliyor.
                var connectionString = "Server=192.168.1.198;Port=3306;Database=ArcelikExcelDb;User=arcelik;Password=ArcelikWifi01;Pooling=true;MinimumPoolSize=2;MaximumPoolSize=10;ConnectionTimeout=15;DefaultCommandTimeout=15;";
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }
    }

    /// <summary>
    /// Veritabanı işlemlerini retry mekanizması ile çalıştırmak için yardımcı sınıf.
    /// </summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Bir veritabanı işlemini otomatik retry ile çalıştırır.
        /// Timeout veya bağlantı hatalarında 3 kez dener.
        /// </summary>
        public static async Task<T> ExecuteWithRetryAsync<T>(Func<AppDbContext, T> operation, int maxRetries = 3)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await Task.Run(() =>
                    {
                        using var db = new AppDbContext();
                        return operation(db);
                    });
                }
                catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
                {
                    lastException = ex;
                    int delayMs = (int)Math.Pow(2, attempt) * 1000;
                    Debug.WriteLine($"DB işlemi başarısız (deneme {attempt}/{maxRetries}): {ex.Message}. {delayMs / 1000}s sonra tekrar deneniyor...");
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }

            throw lastException!;
        }

        /// <summary>
        /// Geçici (transient) bir hata olup olmadığını kontrol eder.
        /// Timeout, bağlantı kopması gibi hatalar geçici olarak kabul edilir.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            string message = ex.Message.ToLowerInvariant();
            string innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";
            string fullMessage = message + " " + innerMessage;

            return fullMessage.Contains("timeout") ||
                   fullMessage.Contains("timed out") ||
                   fullMessage.Contains("connection") ||
                   fullMessage.Contains("unable to connect") ||
                   fullMessage.Contains("network") ||
                   fullMessage.Contains("transport") ||
                   fullMessage.Contains("broken pipe");
        }
    }
}