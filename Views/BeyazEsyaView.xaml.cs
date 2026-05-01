using ArcelikApp.Data;
using ArcelikApp.Models;
using ArcelikApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArcelikExcelApp.Views
{
    public partial class BeyazEsyaView : UserControl
    {
        private List<CostCalculation> _allData = new();
        private List<CostCalculation> _filteredData = new();
        
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalPages = 1;

        public BeyazEsyaView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadDataAsync();
            // Sayfa yüklendiğinde Ctrl+F dinleyebilmesi için odaklan
            this.Focus();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                OverlayLoading.Visibility = Visibility.Visible;
                _allData = await Task.Run(() =>
                {
                    using var db = new AppDbContext();

                    var firstCalc = db.CostCalculations.FirstOrDefault();
                    int markupPercent = firstCalc != null ? Convert.ToInt32(firstCalc.CardMarkupPercent) : 10;
                    Dispatcher.Invoke(() =>
                    {
                        ColCardPrice.Header = $"Kart Fiyatı (%{markupPercent})";
                    });

                    // Beyaz Eşya kategorisindeki maliyetleri getir (en son hesaplananlar üstte)
                    return db.CostCalculations
                        .Where(c => c.SourceTable == "WhiteGoods")
                        .OrderByDescending(c => c.Id)
                        .ToList();
                });

                FilterData();
            }
            catch (Exception ex)
            {
                await ModernDialogService.ShowAsync("Hata", $"Veriler yüklenirken hata oluştu: {ex.Message}", ModernDialogType.Error);
            }
            finally
            {
                OverlayLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        private void FilterData()
        {
            string query = TxtSearch.Text.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(query))
            {
                _filteredData = _allData;
            }
            else
            {
                _filteredData = _allData.Where(c => 
                    c.ProductCode.ToLowerInvariant().Contains(query) || 
                    c.ProductName.ToLowerInvariant().Contains(query)).ToList();
            }

            TxtTotalCount.Text = $"Toplam: {_filteredData.Count} Ürün";
            
            _totalPages = (int)Math.Ceiling((double)_filteredData.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;
            
            _currentPage = 1;
            UpdateGrid();
        }

        private void UpdateGrid()
        {
            var pagedData = _filteredData
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            GridBeyazEsya.ItemsSource = pagedData;
            TxtPageInfo.Text = $"Sayfa {_currentPage} / {_totalPages}";

            BtnPrev.IsEnabled = _currentPage > 1;
            BtnNext.IsEnabled = _currentPage < _totalPages;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateGrid();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdateGrid();
            }
        }

        // Ctrl + F Kısayolu için
        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
                e.Handled = true;
            }
        }

        private async void MenuItem_OpenExcel_Click(object sender, RoutedEventArgs e)
        {
            if (GridBeyazEsya.SelectedItem is CostCalculation calc)
            {
                try
                {
                    using var db = new AppDbContext();
                    int? fileId = null;
                    
                    // 1. Önce ProductId (ID) ile ara
                    if (int.TryParse(calc.ProductId, out int prodId) && prodId > 0)
                    {
                        var prod = db.WhiteGoodsProducts.FirstOrDefault(x => x.Id == prodId);
                        if (prod != null) fileId = prod.UploadedFileId;
                    }

                    // 2. Bulunamazsa ProductCode ile ara (Daha sağlam)
                    if (fileId == null)
                    {
                        var prod = db.WhiteGoodsProducts
                            .OrderByDescending(x => x.Id) // En son eklenen aynı kodlu ürünü al
                            .FirstOrDefault(x => x.ProductCode == calc.ProductCode);
                        if (prod != null) fileId = prod.UploadedFileId;
                    }

                    if (fileId.HasValue)
                    {
                        var window = Window.GetWindow(this) as MainWindow;
                        if (window != null)
                        {
                            var viewer = new ExcelViewer();
                            window.MainContentControl.Content = viewer;
                            window.TxtPageTitle.Text = "Excel Görüntüleyici";
                            
                            // Menüdeki aktif butonu güncelle
                            if (window.MenuStackPanel != null)
                            {
                                foreach (var child in window.MenuStackPanel.Children)
                                {
                                    if (child is Button menuBtn)
                                    {
                                        if (menuBtn.Tag as string == "ExcelViewer")
                                        {
                                            menuBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E02020"));
                                            menuBtn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                                        }
                                        else
                                        {
                                            menuBtn.ClearValue(Button.BackgroundProperty);
                                            menuBtn.ClearValue(Button.ForegroundProperty);
                                        }
                                    }
                                }
                            }
                            
                            viewer.LoadSpecificFile(fileId.Value);
                        }
                    }
                    else
                    {
                        await ModernDialogService.ShowAsync("Hata", "Bu ürüne ait kaynak Excel dosyası bulunamadı.", ModernDialogType.Warning);
                    }
                }
                catch (Exception ex)
                {
                    await ModernDialogService.ShowAsync("Hata", $"Dosya açılırken hata oluştu: {ex.Message}", ModernDialogType.Error);
                }
            }
        }
        private async void MenuItem_OpenInArcelik_Click(object sender, RoutedEventArgs e)
        {
            if (GridBeyazEsya.SelectedItem is CostCalculation calc)
            {
                try
                {
                    string searchCode = calc.ProductCode; // Default olarak ProductCode
                    
                    // Veritabanından WhiteGoodsProduct çekilip Klima ise Description alınacak
                    using (var db = new AppDbContext())
                    {
                        WhiteGoodsProduct? wgProduct = null;
                        if (int.TryParse(calc.ProductId, out int prodId) && prodId > 0)
                        {
                            wgProduct = db.WhiteGoodsProducts.FirstOrDefault(x => x.Id == prodId);
                        }
                        
                        if (wgProduct == null)
                        {
                            wgProduct = db.WhiteGoodsProducts.OrderByDescending(x => x.Id).FirstOrDefault(x => x.ProductCode == calc.ProductCode);
                        }

                        if (wgProduct != null && wgProduct.ExcelFileType == "Klima" && !string.IsNullOrWhiteSpace(wgProduct.Description))
                        {
                            searchCode = wgProduct.Description; // Dış Ünite SKU
                        }
                    }

                    string url = $"https://www.arcelik.com.tr/arama?q={searchCode}";
                    await BrowserHelper.OpenUrlAsync(url);
                }
                catch (Exception ex)
                {
                    await ModernDialogService.ShowAsync("Hata", $"İşlem sırasında bir hata oluştu: {ex.Message}", ModernDialogType.Error);
                }
            }
        }

        private async void MenuItem_ViewAllValors_Click(object sender, RoutedEventArgs e)
        {
            if (GridBeyazEsya.SelectedItem is CostCalculation calc)
            {
                try
                {
                    OverlayLoading.Visibility = Visibility.Visible;
                    
                    WhiteGoodsProduct? wgProduct = null;
                    await Task.Run(() =>
                    {
                        using var db = new AppDbContext();
                        // Önce ID ile ara
                        if (int.TryParse(calc.ProductId, out int prodId) && prodId > 0)
                        {
                            wgProduct = db.WhiteGoodsProducts.FirstOrDefault(x => x.Id == prodId);
                        }
                        
                        // Bulunamazsa Ürün Kodu ile en güncelini al
                        if (wgProduct == null)
                        {
                            wgProduct = db.WhiteGoodsProducts
                                .OrderByDescending(x => x.Id)
                                .FirstOrDefault(x => x.ProductCode == calc.ProductCode);
                        }
                    });

                    OverlayLoading.Visibility = Visibility.Collapsed;

                    if (wgProduct == null)
                    {
                        await ModernDialogService.ShowAsync("Hata", "Bu ürüne ait kaynak detaylar veritabanında bulunamadı.", ModernDialogType.Warning);
                        return;
                    }

                    TxtValorProductName.Text = $"{calc.ProductCode} - {calc.ProductName}";
                    PnlValorContainer.Children.Clear();

                    var valors = new[]
                    {
                        new { Label = wgProduct.ExcelFileType == "Klima" ? "30 Günlük" : "30 Günlük", Price = wgProduct.WholesalePrice30 },
                        new { Label = wgProduct.ExcelFileType == "Klima" ? "Y060" : "60 Günlük", Price = wgProduct.WholesalePrice60 },
                        new { Label = wgProduct.ExcelFileType == "Klima" ? "Y90" : "90 Günlük", Price = wgProduct.WholesalePrice90 },
                        new { Label = wgProduct.ExcelFileType == "Klima" ? "Y120" : "120 Günlük", Price = wgProduct.WholesalePrice120 }
                    };

                    bool hasAnyData = false;

                    foreach (var valor in valors)
                    {
                        if (valor.Price.HasValue && valor.Price.Value > 0)
                        {
                            hasAnyData = true;
                            
                            // Hesaplamalar
                            decimal priceConversion = calc.PriceConversion; // İndirim miktarı
                            decimal purchasePrice = valor.Price.Value - priceConversion;
                            decimal finalCost = Math.Round(purchasePrice * (1 + calc.CardMarkupPercent / 100m), 2);

                            // UI Elemanlarını Oluştur
                            var border = new Border
                            {
                                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F1F5F9")),
                                CornerRadius = new CornerRadius(12),
                                Padding = new Thickness(20, 15, 20, 15),
                                Margin = new Thickness(0, 0, 0, 12)
                            };

                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                            titleStack.Children.Add(new TextBlock 
                            { 
                                Text = $"{valor.Label} Valör Maliyeti", 
                                FontWeight = FontWeights.Bold, 
                                FontSize = 15, 
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E293B")) 
                            });
                            
                            titleStack.Children.Add(new TextBlock 
                            { 
                                Text = $"Baz: {valor.Price.Value:N2} ₺ | İndirim: {priceConversion:N2} ₺", 
                                FontSize = 12, 
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B")),
                                Margin = new Thickness(0, 4, 0, 0)
                            });

                            titleStack.Children.Add(new TextBlock 
                            { 
                                Text = $"Net Maliyet: {purchasePrice:N2} ₺", 
                                FontWeight = FontWeights.Bold,
                                FontSize = 14, 
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981")), // Yeşil renk
                                Margin = new Thickness(0, 6, 0, 0)
                            });

                            var costStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
                            
                            costStack.Children.Add(new TextBlock
                            {
                                Text = "Kredi Kartı",
                                FontSize = 12,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B")),
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Margin = new Thickness(0, 0, 0, 2)
                            });

                            costStack.Children.Add(new TextBlock
                            {
                                Text = $"{finalCost:N2} ₺",
                                FontWeight = FontWeights.ExtraBold,
                                FontSize = 18,
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E02020")),
                                HorizontalAlignment = HorizontalAlignment.Right
                            });
                            
                            costStack.Children.Add(new TextBlock
                            {
                                Text = $"%{(int)calc.CardMarkupPercent} Komisyon",
                                FontSize = 11,
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8")),
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Margin = new Thickness(0, 2, 0, 0)
                            });

                            Grid.SetColumn(titleStack, 0);
                            Grid.SetColumn(costStack, 1);

                            grid.Children.Add(titleStack);
                            grid.Children.Add(costStack);
                            border.Child = grid;

                            PnlValorContainer.Children.Add(border);
                        }
                    }

                    if (!hasAnyData)
                    {
                        PnlValorContainer.Children.Add(new TextBlock
                        {
                            Text = "Bu ürün için tanımlanmış ek valör fiyatları bulunamadı.",
                            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 14,
                            Margin = new Thickness(0, 30, 0, 30)
                        });
                    }

                    ValorDialogOverlay.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    OverlayLoading.Visibility = Visibility.Collapsed;
                    await ModernDialogService.ShowAsync("Hata", $"Valörler hesaplanırken beklenmeyen bir hata oluştu:\n{ex.Message}", ModernDialogType.Error);
                }
            }
        }

        private void BtnCloseValorDialog_Click(object sender, RoutedEventArgs e)
        {
            ValorDialogOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
