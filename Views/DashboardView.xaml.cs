using ArcelikApp.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArcelikExcelApp.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var data = await Task.Run(() =>
                {
                    using var db = new AppDbContext();

                    // 1. Temel Sayısal Veriler
                    int keaCount = db.KeaProducts.Count();
                    int wgCount = db.WhiteGoodsProducts.Count();
                    int totalProducts = keaCount + wgCount;
                    
                    int totalCampaigns = db.OlizCampaigns.Count();
                    int totalCalculations = db.CostCalculations.Count();
                    int totalFiles = db.UploadedFiles.Count();

                    // 2. Ortalama Fiyat Analizleri
                    decimal keaAvg = 0;
                    if (keaCount > 0)
                    {
                        keaAvg = db.KeaProducts.Where(x => x.WholesalePrice60 > 0).Average(x => (decimal?)x.WholesalePrice60) ?? 0;
                    }
                    
                    decimal wgAvg = 0;
                    if (wgCount > 0)
                    {
                        wgAvg = db.WhiteGoodsProducts.Where(x => x.WholesalePrice60 > 0).Average(x => (decimal?)x.WholesalePrice60) ?? 0;
                    }

                    // 3. Kampanya Kullanım Oranı
                    int campaignAppliedCount = 0;
                    double campaignRatio = 0;
                    if (totalCalculations > 0)
                    {
                        campaignAppliedCount = db.CostCalculations.Count(x => x.PriceConversion > 0);
                        campaignRatio = ((double)campaignAppliedCount / totalCalculations) * 100;
                    }

                    // 4. Kategori Dağılımı
                    double keaRatio = totalProducts > 0 ? ((double)keaCount / totalProducts) * 100 : 0;
                    double wgRatio = totalProducts > 0 ? ((double)wgCount / totalProducts) * 100 : 0;

                    // 5. Son Yapılan Hesaplamalar (DataGrid)
                    var recentCalculations = db.CostCalculations
                                               .OrderByDescending(x => x.Id)
                                               .Take(15)
                                               .ToList();

                    return new
                    {
                        keaCount, wgCount, totalProducts, totalCampaigns, totalCalculations, totalFiles,
                        keaAvg, wgAvg, campaignRatio, keaRatio, wgRatio, recentCalculations
                    };
                });

                // UI güncellemelerini ana thread'de yap
                TxtTotalProducts.Text = data.totalProducts.ToString("N0");
                TxtKeaCount.Text = data.keaCount.ToString("N0");
                TxtWgCount.Text = data.wgCount.ToString("N0");
                TxtTotalFiles.Text = data.totalFiles.ToString("N0");
                TxtTotalCampaigns.Text = data.totalCampaigns.ToString("N0");
                TxtTotalCalculations.Text = $"Toplam: {data.totalCalculations:N0}";

                TxtKeaAvgPrice.Text = $"{data.keaAvg:N0} ₺";
                TxtWgAvgPrice.Text = $"{data.wgAvg:N0} ₺";

                TxtCampaignRatio.Text = $"%{data.campaignRatio:F1}";
                ProgCampaign.Value = data.campaignRatio;

                TxtKeaRatio.Text = $"%{data.keaRatio:F1}";
                ProgKea.Value = data.keaRatio;

                TxtWgRatio.Text = $"%{data.wgRatio:F1}";
                ProgWg.Value = data.wgRatio;

                GridRecentCalculations.ItemsSource = data.recentCalculations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dashboard verileri yüklenirken hata oluştu: {ex.Message}", "Veri Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

