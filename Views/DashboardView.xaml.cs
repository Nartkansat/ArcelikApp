using ArcelikApp.Data;
using System;
using System.Linq;
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
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                using var db = new AppDbContext();

                // 1. Temel Sayısal Veriler
                int keaCount = db.KeaProducts.Count();
                int wgCount = db.WhiteGoodsProducts.Count();
                int totalProducts = keaCount + wgCount;
                
                int totalCampaigns = db.OlizCampaigns.Count();
                int totalCalculations = db.CostCalculations.Count();
                int totalFiles = db.UploadedFiles.Count();

                TxtTotalProducts.Text = totalProducts.ToString("N0");
                TxtKeaCount.Text = keaCount.ToString("N0");
                TxtWgCount.Text = wgCount.ToString("N0");
                TxtTotalFiles.Text = totalFiles.ToString("N0");
                TxtTotalCampaigns.Text = totalCampaigns.ToString("N0");
                TxtTotalCalculations.Text = $"Toplam: {totalCalculations:N0}";

                // 2. Ortalama Fiyat Analizleri
                decimal keaAvg = 0;
                if (keaCount > 0)
                {
                    // Boş olmayan WholesalePrice60 değerlerinin ortalaması
                    keaAvg = db.KeaProducts.Where(x => x.WholesalePrice60 > 0).Average(x => (decimal?)x.WholesalePrice60) ?? 0;
                }
                
                decimal wgAvg = 0;
                if (wgCount > 0)
                {
                    wgAvg = db.WhiteGoodsProducts.Where(x => x.WholesalePrice60 > 0).Average(x => (decimal?)x.WholesalePrice60) ?? 0;
                }

                TxtKeaAvgPrice.Text = $"{keaAvg:N0} ₺";
                TxtWgAvgPrice.Text = $"{wgAvg:N0} ₺";

                // 3. Kampanya Kullanım Oranı
                if (totalCalculations > 0)
                {
                    int campaignAppliedCount = db.CostCalculations.Count(x => x.PriceConversion > 0);
                    double ratio = ((double)campaignAppliedCount / totalCalculations) * 100;
                    TxtCampaignRatio.Text = $"%{ratio:F1}";
                    ProgCampaign.Value = ratio;
                }
                else
                {
                    TxtCampaignRatio.Text = "%0";
                    ProgCampaign.Value = 0;
                }

                // 4. Kategori Dağılımı
                if (totalProducts > 0)
                {
                    double keaRatio = ((double)keaCount / totalProducts) * 100;
                    double wgRatio = ((double)wgCount / totalProducts) * 100;

                    TxtKeaRatio.Text = $"%{keaRatio:F1}";
                    ProgKea.Value = keaRatio;

                    TxtWgRatio.Text = $"%{wgRatio:F1}";
                    ProgWg.Value = wgRatio;
                }
                else
                {
                    TxtKeaRatio.Text = "%0";
                    ProgKea.Value = 0;
                    TxtWgRatio.Text = "%0";
                    ProgWg.Value = 0;
                }

                // 5. Son Yapılan Hesaplamalar (DataGrid)
                var recentCalculations = db.CostCalculations
                                           .OrderByDescending(x => x.Id)
                                           .Take(15)
                                           .ToList();
                                           
                GridRecentCalculations.ItemsSource = recentCalculations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dashboard verileri yüklenirken hata oluştu: {ex.Message}", "Veri Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
