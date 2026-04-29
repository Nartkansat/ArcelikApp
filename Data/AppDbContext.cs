using ArcelikApp.Models;
using Microsoft.EntityFrameworkCore;
using System;

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

        public AppDbContext()
        {
        }

        public static bool TestConnection()
        {
            try
            {
                using var db = new AppDbContext();
                return db.Database.CanConnect();
            }
            catch
            {
                return false;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                // Anabilgisayar ip adresi, db ye oradan baglaniliyor.
                var connectionString = "Server=192.168.1.198;Port=3306;Database=ArcelikExcelDb;User=arcelik;Password=ArcelikWifi01;";
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }
    }
}