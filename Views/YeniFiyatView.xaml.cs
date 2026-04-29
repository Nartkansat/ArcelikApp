using ArcelikApp.Data;
using ArcelikApp.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArcelikApp.Services;

namespace ArcelikExcelApp.Views
{
    public partial class YeniFiyatView : UserControl
    {
        // -------------------------------------------------------------------
        // DTO — DataGrid'e bağlanan satır
        // -------------------------------------------------------------------
        private class CostRow
        {
            public string ProductId { get; set; } = string.Empty;
            public string ProductCode { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Kategori { get; set; } = string.Empty;       // "KEA" veya "Beyaz Eşya"
            public string PricePPSource { get; set; } = string.Empty;
            public decimal PricePP { get; set; }
            public decimal PriceConversion { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal CardMarkupPercent { get; set; }
            public decimal CardPurchasePrice { get; set; }
            public bool HasCampaign { get; set; }
            public string HasCampaignDisplay => HasCampaign ? "✔ Evet" : "✘ Hayır";
            public string CampaingDate { get; set; } = string.Empty;
        }

        private class FileSelectionWrapper : System.ComponentModel.INotifyPropertyChanged
        {
            private bool _isSelected;
            public UploadedFile File { get; set; } = new();
            public string FileName => File.FileName;
            public int Id => File.Id;
            public bool IsSelected 
            { 
                get => _isSelected; 
                set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); } 
            }
            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        }

        private List<CostRow> _currentRows = new();
        private List<FileSelectionWrapper> _allProductFiles = new();

        public YeniFiyatView()
        {
            InitializeComponent();
        }

        // -------------------------------------------------------------------
        // Loaded
        // -------------------------------------------------------------------
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadOlizFilesAsync();
            _ = LoadProductFilesAsync("KEA"); // varsayılan kategori
        }

        // -------------------------------------------------------------------
        // Oliz dosyalarını yükle
        // -------------------------------------------------------------------
        private async Task LoadOlizFilesAsync()
        {
            try
            {
                var olizFiles = await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    return db.UploadedFiles
                        .Where(f => f.Category == "Oliz Kampanya")
                        .OrderByDescending(f => f.Id)
                        .ToList();
                });

                CmbOlizFile.ItemsSource = olizFiles;
                if (olizFiles.Any())
                    CmbOlizFile.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                SetStatus($"DB bağlantı hatası: {ex.Message}", isError: true);
            }
        }

        // -------------------------------------------------------------------
        // Ürün dosyalarını kategoriye göre yükle
        // -------------------------------------------------------------------
        private async Task LoadProductFilesAsync(string category)
        {
            try
            {
                var files = await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    
                    if (category == "Tümü")
                    {
                        return db.UploadedFiles
                            .Where(f => f.Category == "Kea" || f.Category == "BeyazEsya" || f.Category == "Beyaz Eşya")
                            .OrderByDescending(f => f.Id)
                            .ToList();
                    }
                    else if (category == "Beyaz Eşya")
                    {
                        return db.UploadedFiles
                            .Where(f => f.Category == "BeyazEsya" || f.Category == "Beyaz Eşya")
                            .OrderByDescending(f => f.Id)
                            .ToList();
                    }
                    else // KEA
                    {
                        return db.UploadedFiles
                            .Where(f => f.Category == "Kea")
                            .OrderByDescending(f => f.Id)
                            .ToList();
                    }
                });

                _allProductFiles = files.Select(f => new FileSelectionWrapper { File = f, IsSelected = true }).ToList();
                UpdateProductFilesList();
                UpdateSelectedFilesCount();
            }
            catch (Exception ex)
            {
                SetStatus($"Dosya listesi hatası: {ex.Message}", isError: true);
            }
        }

        private void UpdateProductFilesList()
        {
            string search = TxtSearchProductFiles?.Text?.ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(search) 
                ? _allProductFiles 
                : _allProductFiles.Where(f => f.FileName.ToLower().Contains(search)).ToList();
            
            LstProductFiles.ItemsSource = null;
            LstProductFiles.ItemsSource = filtered;
        }

        private void UpdateSelectedFilesCount()
        {
            int selectedCount = _allProductFiles.Count(f => f.IsSelected);
            if (selectedCount == 0) TxtSelectedFilesCount.Text = "Seçim Yapılmadı";
            else if (selectedCount == _allProductFiles.Count && _allProductFiles.Count > 0) TxtSelectedFilesCount.Text = "Tüm Dosyalar";
            else TxtSelectedFilesCount.Text = $"{selectedCount} Dosya Seçildi";
        }

        private void TxtSearchProductFiles_TextChanged(object sender, TextChangedEventArgs e) => UpdateProductFilesList();

        private void ChkSelectAllFiles_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = ChkSelectAllFiles.IsChecked ?? false;
            foreach (var f in _allProductFiles) f.IsSelected = isChecked;
            UpdateSelectedFilesCount();
        }

        private void FileCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateSelectedFilesCount();

        private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
        {
            PopupFileSelection.IsOpen = !PopupFileSelection.IsOpen;
        }

        private void BtnApplySelection_Click(object sender, RoutedEventArgs e)
        {
            PopupFileSelection.IsOpen = false;
        }

        // -------------------------------------------------------------------
        // Kategori değişince dosya listesini yenile
        // -------------------------------------------------------------------
        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCategory?.SelectedItem is ComboBoxItem item)
                _ = LoadProductFilesAsync(item.Content?.ToString() ?? "KEA");
        }

        // -------------------------------------------------------------------
        // PricePP kaynağı değişince bilgi ver
        // -------------------------------------------------------------------
        private void CmbPricePPSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbPricePPSource?.SelectedItem is ComboBoxItem item)
                SetStatus($"PricePP kaynağı: {item.Tag} olarak seçildi.");
        }

        // -------------------------------------------------------------------
        // HESAPLA
        // -------------------------------------------------------------------
        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbOlizFile.SelectedItem == null)
                {
                    _ = ModernDialogService.ShowAsync("Uyarı", "Lütfen bir Oliz Kampanya dosyası seçin.", ModernDialogType.Warning);
                    return;
                }

                string pricePPSource = "WholesalePrice60";
                if (CmbPricePPSource.SelectedItem is ComboBoxItem selItem)
                    pricePPSource = selItem.Tag?.ToString() ?? "WholesalePrice60";

                if (!decimal.TryParse(TxtCardMarkup.Text.Replace(",", "."), out decimal markupPct))
                    markupPct = 10m;

                string selectedCategory = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "KEA";
                int? selectedOlizFileId = (CmbOlizFile.SelectedItem as UploadedFile)?.Id;
                
                var selectedProductFileIds = _allProductFiles
                    .Where(f => f.IsSelected)
                    .Select(f => f.Id)
                    .ToList();

                if (!selectedProductFileIds.Any())
                {
                    _ = ModernDialogService.ShowAsync("Uyarı", "Lütfen en az bir ürün dosyası seçin.", ModernDialogType.Warning);
                    return;
                }

                SetStatus("Hesaplanıyor...");
                BtnCalculate.IsEnabled = false;

                using var db = new AppDbContext();

                // --- Kampanya tablosunu yükle ---
                var campaigns = db.OlizCampaigns
                    .Where(c => c.UploadedFileId == selectedOlizFileId)
                    .ToList();

                var campaignLookup = campaigns
                    .GroupBy(c => c.ProductCode.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                var rows = new List<CostRow>();

                // --- KEA ürünleri ---
                if (selectedCategory == "KEA" || selectedCategory == "Tümü")
                {
                    var keaProducts = db.KeaProducts
                        .Where(k => selectedProductFileIds.Contains(k.UploadedFileId))
                        .ToList();

                    foreach (var kea in keaProducts)
                        rows.Add(BuildRow(kea.Id.ToString(), kea.ProductCode, kea.Description, kea.ProductName, "KEA",
                                          GetPricePP(kea, pricePPSource), pricePPSource,
                                          markupPct, campaignLookup));
                }

                // --- Beyaz Eşya ürünleri ---
                if (selectedCategory == "Beyaz Eşya" || selectedCategory == "Tümü")
                {
                    var wgProducts = db.WhiteGoodsProducts
                        .Where(w => selectedProductFileIds.Contains(w.UploadedFileId))
                        .ToList();

                    foreach (var wg in wgProducts)
                        rows.Add(BuildRow(wg.Id.ToString(), wg.ProductCode, wg.Description, wg.ProductName, "Beyaz Eşya",
                                          GetPricePPWG(wg, pricePPSource), pricePPSource,
                                          markupPct, campaignLookup));
                }

                if (!rows.Any())
                {
                    _ = ModernDialogService.ShowAsync("Bilgi", "Seçilen kriterlere göre ürün bulunamadı.", ModernDialogType.Info);
                    BtnCalculate.IsEnabled = true;
                    return;
                }

                _currentRows = rows;
                GridResults.ItemsSource = rows;

                int campaignCount   = rows.Count(r => r.HasCampaign);
                int noCampaignCount = rows.Count(r => !r.HasCampaign);

                TxtResultSummary.Text   = $"{rows.Count} ürün hesaplandı";
                TxtCampaignCount.Text   = campaignCount.ToString();
                TxtNoCampaignCount.Text = noCampaignCount.ToString();

                BtnSave.IsEnabled   = true;
                BtnExport.IsEnabled = true;
                BtnCalculate.IsEnabled = true;

                SetStatus($"✔ Tamamlandı — {rows.Count} ürün ({campaignCount} kampanyalı, {noCampaignCount} kampanyasız).");
            }
            catch (Exception ex)
            {
                BtnCalculate.IsEnabled = true;
                SetStatus($"Hata: {ex.Message}", isError: true);
                _ = ModernDialogService.ShowAsync("Hata", $"Hesaplama hatası:\n{ex.Message}", ModernDialogType.Error);
            }
        }

        // -------------------------------------------------------------------
        // Ortak satır oluşturucu
        // -------------------------------------------------------------------
        private static CostRow BuildRow(
            string productId, string productCode, string description, string productName, string kategori,
            decimal pricePP, string pricePPSource, decimal markupPct,
            Dictionary<string, OlizCampaign> campaignLookup)
        {
            OlizCampaign campaign = null;
            string codeKey = productCode?.Trim().ToUpperInvariant() ?? string.Empty;
            bool hasCampaign = false;

            if (!string.IsNullOrEmpty(codeKey))
            {
                hasCampaign = campaignLookup.TryGetValue(codeKey, out campaign);
            }

            if (!hasCampaign && !string.IsNullOrWhiteSpace(description))
            {
                string descKey = description.Trim().ToUpperInvariant();
                hasCampaign = campaignLookup.TryGetValue(descKey, out campaign);
            }

            decimal priceConversion   = hasCampaign ? campaign!.DiscountNetAmount : 0m;
            decimal purchasePrice     = pricePP - priceConversion;
            decimal cardPurchasePrice = Math.Round(purchasePrice * (1 + markupPct / 100m), 2);
            string campaignDate       = hasCampaign
                ? $"{campaign!.CampaignStartDate} - {campaign.CampaignEndDate}"
                : string.Empty;

            return new CostRow
            {
                ProductId         = productId,
                ProductCode       = productCode,
                ProductName       = productName,
                Kategori          = kategori,
                PricePPSource     = pricePPSource,
                PricePP           = pricePP,
                PriceConversion   = priceConversion,
                PurchasePrice     = purchasePrice,
                CardMarkupPercent = markupPct,
                CardPurchasePrice = cardPurchasePrice,
                HasCampaign       = hasCampaign,
                CampaingDate      = campaignDate
            };
        }

        // -------------------------------------------------------------------
        // DB'YE KAYDET
        // -------------------------------------------------------------------
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentRows.Any()) return;

            try
            {
                BtnSave.IsEnabled = false;
                SetStatus("Veritabanına kaydediliyor...");

                using var db = new AppDbContext();

                var entities = _currentRows.Select(r => new CostCalculation
                {
                    ProductId         = r.ProductId,
                    ProductCode       = r.ProductCode,
                    ProductName       = r.ProductName,
                    SourceTable       = r.Kategori == "KEA" ? "Kea" : "WhiteGoods",
                    PricePPSource     = r.PricePPSource,
                    PricePP           = r.PricePP,
                    PriceConversion   = r.PriceConversion,
                    PurchasePrice     = r.PurchasePrice,
                    CardMarkupPercent = r.CardMarkupPercent,
                    CardPurchasePrice = r.CardPurchasePrice,
                    CampaingDate      = r.CampaingDate,
                    CreatedDate       = DateTime.Now
                }).ToList();

                db.CostCalculations.AddRange(entities);
                db.SaveChanges();

                SetStatus($"✔ {entities.Count} kayıt veritabanına eklendi.");
                _ = ModernDialogService.ShowAsync("Başarılı", $"{entities.Count} kayıt başarıyla kaydedildi!", ModernDialogType.Success);
            }
            catch (Exception ex)
            {
                SetStatus($"Kayıt hatası: {ex.Message}", isError: true);
                _ = ModernDialogService.ShowAsync("Hata", $"Kayıt hatası:\n{ex.Message}", ModernDialogType.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        // -------------------------------------------------------------------
        // EXCEL'E AKTAR
        // -------------------------------------------------------------------
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentRows.Any()) return;

            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title    = "Excel Dosyasını Kaydet",
                    Filter   = "Excel Dosyası (*.xlsx)|*.xlsx",
                    FileName = $"MaliyetHesabi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };
                if (dlg.ShowDialog() != true) return;

                ExcelPackage.License.SetNonCommercialPersonal("ArcelikApp");
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Maliyet Hesabı");

                string[] headers = {
                    "Kategori", "Ürün Kodu", "Ürün Adı", "PricePP Kaynağı", "PricePP (₺)",
                    "Kampanya İndirimi (₺)", "Maliyet Fiyatı (₺)", "Markup %", "Kart Fiyatı (₺)",
                    "Kampanyalı mı?", "Kampanya Dönemi"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                    ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(28, 43, 74));
                    ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                for (int r = 0; r < _currentRows.Count; r++)
                {
                    var row = _currentRows[r];
                    ws.Cells[r + 2, 1].Value  = row.Kategori;
                    ws.Cells[r + 2, 2].Value  = row.ProductCode;
                    ws.Cells[r + 2, 3].Value  = row.ProductName;
                    ws.Cells[r + 2, 4].Value  = row.PricePPSource;
                    ws.Cells[r + 2, 5].Value  = row.PricePP;
                    ws.Cells[r + 2, 6].Value  = row.PriceConversion;
                    ws.Cells[r + 2, 7].Value  = row.PurchasePrice;
                    ws.Cells[r + 2, 8].Value  = row.CardMarkupPercent;
                    ws.Cells[r + 2, 9].Value  = row.CardPurchasePrice;
                    ws.Cells[r + 2, 10].Value = row.HasCampaign ? "Evet" : "Hayır";
                    ws.Cells[r + 2, 11].Value = row.CampaingDate;

                    foreach (int col in new[] { 5, 6, 7, 9 })
                        ws.Cells[r + 2, col].Style.Numberformat.Format = "#,##0.00 ₺";

                    if (row.HasCampaign)
                    {
                        using var range = ws.Cells[r + 2, 1, r + 2, headers.Length];
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 245, 245));
                    }
                }

                ws.Cells.AutoFitColumns(10, 60);
                package.SaveAs(new FileInfo(dlg.FileName));

                SetStatus($"✔ Excel dosyası kaydedildi: {dlg.FileName}");
                _ = ModernDialogService.ShowAsync("Başarılı", "Excel dosyası başarıyla oluşturuldu!", ModernDialogType.Success);
            }
            catch (Exception ex)
            {
                SetStatus($"Excel hatası: {ex.Message}", isError: true);
                _ = ModernDialogService.ShowAsync("Hata", $"Dışa aktarma hatası:\n{ex.Message}", ModernDialogType.Error);
            }
        }

        // -------------------------------------------------------------------
        // PricePP yardımcıları
        // -------------------------------------------------------------------
        private static decimal GetPricePP(KeaProduct kea, string source) => source switch
        {
            "WholesalePrice30"  => kea.WholesalePrice30  ?? 0m,
            "WholesalePrice60"  => kea.WholesalePrice60  ?? 0m,
            "WholesalePrice90"  => kea.WholesalePrice90  ?? 0m,
            "WholesalePrice120" => kea.WholesalePrice120 ?? 0m,
            "CashPrice"         => kea.CashPrice         ?? 0m,
            _                   => kea.WholesalePrice60  ?? 0m
        };

        private static decimal GetPricePPWG(WhiteGoodsProduct wg, string source) => source switch
        {
            "WholesalePrice30"  => wg.WholesalePrice30  ?? 0m,
            "WholesalePrice60"  => wg.WholesalePrice60  ?? 0m,
            "WholesalePrice90"  => wg.WholesalePrice90  ?? 0m,
            "WholesalePrice120" => wg.WholesalePrice120 ?? 0m,
            "CashPrice"         => wg.CashPrice         ?? 0m,
            _                   => wg.WholesalePrice60  ?? 0m
        };

        // -------------------------------------------------------------------
        // Status bar
        // -------------------------------------------------------------------
        private void SetStatus(string message, bool isError = false)
        {
            if (TxtStatus == null) return;
            TxtStatus.Text = message;
            TxtStatus.Foreground = isError
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
        }
    }
}
