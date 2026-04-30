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
                _allData = await Task.Run(() =>
                {
                    using var db = new AppDbContext();

                    int markupPercent = Convert.ToInt32(db.CostCalculations.FirstOrDefault().CardMarkupPercent); // db.Settings.FirstOrDefault().CardMarkupPercent gibi...
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
                MessageBox.Show($"Veriler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void MenuItem_OpenExcel_Click(object sender, RoutedEventArgs e)
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
                        MessageBox.Show("Bu ürüne ait kaynak Excel dosyası bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dosya açılırken hata oluştu: {ex.Message}");
                }
            }
        }
    }
}
