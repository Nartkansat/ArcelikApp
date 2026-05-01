using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OfficeOpenXml;
using System.Linq;
using ArcelikApp.Data;
using ArcelikApp.Models;
using ArcelikApp.Excel.Mapping;
using ArcelikApp.Excel.Processors;
using System.IO;
using System.Threading.Tasks;
using System;
using ArcelikApp.Services;


namespace ArcelikExcelApp.Views
{
    public partial class ExcelIslemleriView : UserControl
    {
        private string _selectedFilePath = string.Empty;

        public ExcelIslemleriView()
        {
            InitializeComponent();
            ExcelPackage.License.SetNonCommercialPersonal("NART");
            MainSnackbar.MessageQueue = new MaterialDesignThemes.Wpf.SnackbarMessageQueue(TimeSpan.FromSeconds(3));

            // Beyaz Eşya alt tip ComboBox'ını Registry'den doldur
            foreach (var typeName in ColumnMappingRegistry.GetAllFileTypeNames())
                CmbWhiteGoodsType.Items.Add(typeName);

            if (CmbWhiteGoodsType.Items.Count > 0)
                CmbWhiteGoodsType.SelectedIndex = 0;
        }

        // ─── Dosya Seçimi ─────────────────────────────────────────────────────────────
        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Dosyaları|*.xlsx;*.xlsm;*.xls",
                Title  = "Excel Dosyası Seçin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                TxtSelectedFile.Text = Path.GetFileName(_selectedFilePath);
                LoadWorksheetsIfOliz();
            }
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    _selectedFilePath = files[0];
                    TxtSelectedFile.Text = Path.GetFileName(_selectedFilePath);
                    LoadWorksheetsIfOliz();
                }
            }
        }

        // ─── Kategori Değişince Panel Görünürlüklerini Ayarla ─────────────────────────
        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = CmbCategory.SelectedItem as ComboBoxItem;
            string category  = selectedItem?.Content?.ToString() ?? "";

            PnlWorksheet.Visibility      = (category == "Oliz Kampanya") ? Visibility.Visible : Visibility.Collapsed;
            
            // Beyaz Eşya veya Kea seçilirse Tip panelini göster
            bool showTypePanel = (category == "Beyaz Eşya" || category == "Kea");
            PnlWhiteGoodsType.Visibility = showTypePanel ? Visibility.Visible : Visibility.Collapsed;

            if (showTypePanel)
            {
                CmbWhiteGoodsType.Items.Clear();
                foreach (var typeName in ColumnMappingRegistry.GetTypesByCategory(category))
                    CmbWhiteGoodsType.Items.Add(typeName);

                if (CmbWhiteGoodsType.Items.Count > 0)
                    CmbWhiteGoodsType.SelectedIndex = 0;
            }

            LoadWorksheetsIfOliz();
        }

        // ─── Oliz için Çalışma Sayfalarını Yükle ──────────────────────────────────────
        private async void LoadWorksheetsIfOliz()
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath)) return;

            var selectedItem = CmbCategory.SelectedItem as ComboBoxItem;
            if (selectedItem?.Content?.ToString() != "Oliz Kampanya") return;

            CmbWorksheet.Items.Clear();

            if (FileHelper.IsFileLocked(_selectedFilePath))
            {
                MainSnackbar.MessageQueue?.Enqueue("⚠️ Excel dosyası açık olduğu için sayfalar okunamadı.");
                return;
            }

            try
            {
                using var package = new ExcelPackage(new FileInfo(_selectedFilePath));
                foreach (var worksheet in package.Workbook.Worksheets)
                    CmbWorksheet.Items.Add(worksheet.Name);

                if (CmbWorksheet.Items.Count > 0)
                    CmbWorksheet.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                await ModernDialogService.ShowAsync("Excel Hatası", $"Excel sayfaları yüklenirken hata oluştu:\n{ex.Message}", ModernDialogType.Error);
            }
        }

        // ─── Yükle ve İşle ────────────────────────────────────────────────────────────
        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MainSnackbar.MessageQueue?.Enqueue("Lütfen önce bir dosya seçin.");
                return;
            }

            if (CmbCategory.SelectedItem == null)
            {
                MainSnackbar.MessageQueue?.Enqueue("Lütfen bir kategori seçin.");
                return;
            }

            var categoryItem = CmbCategory.SelectedItem as ComboBoxItem;
            string categoryName = categoryItem?.Content?.ToString() ?? "";

            // Oliz: çalışma sayfası seçili mi?
            string worksheetName = "";
            if (categoryName == "Oliz Kampanya")
            {
                if (CmbWorksheet.SelectedItem == null)
                {
                    MainSnackbar.MessageQueue?.Enqueue("Lütfen bir çalışma sayfası seçin.");
                    return;
                }
                worksheetName = CmbWorksheet.SelectedItem.ToString()!;
            }

            // Beyaz Eşya veya Kea: alt tip seçili mi?
            string whiteGoodsType = "";
            if (categoryName == "Beyaz Eşya" || categoryName == "Kea")
            {
                if (CmbWhiteGoodsType.SelectedItem == null)
                {
                    MainSnackbar.MessageQueue?.Enqueue("Lütfen bir ürün tipi seçin.");
                    return;
                }
                whiteGoodsType = CmbWhiteGoodsType.SelectedItem.ToString()!;
            }

            // Uzantı kontrolü (.xlsx)
            string extension = System.IO.Path.GetExtension(_selectedFilePath).ToLower();
            if (extension != ".xlsx")
            {
                await ModernDialogService.ShowAsync("Geçersiz Dosya", "Sadece .xlsx uzantılı dosyalar desteklenmektedir.", ModernDialogType.Warning);
                return;
            }

            // İsim çakışması kontrolü
            string fileName = System.IO.Path.GetFileName(_selectedFilePath);
            using (var context = new ArcelikApp.Data.AppDbContext())
            {
                if (context.UploadedFiles.Any(f => f.FileName == fileName))
                {
                    await ModernDialogService.ShowAsync("Dosya Zaten Mevcut", $"'{fileName}' isimli bir dosya zaten yüklü. Lütfen farklı bir isimle deneyin veya mevcut dosyayı Dosya Yönetimi panelinden silin.", ModernDialogType.Warning);
                    return;
                }
            }

            // Dosya kilitli mi kontrolü (Excel'de açıksa hata verir)
            if (FileHelper.IsFileLocked(_selectedFilePath))
            {
                MainSnackbar.MessageQueue?.Enqueue("⚠️ Seçilen Excel dosyası şu an açık! Lütfen dosyayı kapatıp tekrar deneyin.");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            BtnUpload.IsEnabled       = false;

            int importedCount = 0;

            try
            {
                await Task.Run(() =>
                {
                    using var context = new AppDbContext();

                    // Dosyayı uygulama içine kopyala ve göreli yolu al
                    string relativePath = FileHelper.CopyToStorage(_selectedFilePath);

                    var newFile = new UploadedFile
                    {
                        FileName   = Path.GetFileName(_selectedFilePath),
                        FilePath   = relativePath,
                        FileData   = File.ReadAllBytes(_selectedFilePath),
                        Category   = categoryName,
                        UploadDate = DateTime.Now.ToString("dd.MM.yyyy")
                    };
                    context.UploadedFiles.Add(newFile);
                    context.SaveChanges();

                    // ilgili excell açıksa program hatası verir. burada bir sorgu

                    // İşleme için artık kopyalanan dosyayı kullanabiliriz
                    string absoluteStoragePath = FileHelper.GetAbsolutePath(relativePath);
                    
                    // Geçici olarak _selectedFilePath'i bu yeni yol ile güncelleyelim ki işlemci oradan okusun
                    // (Veya metodlara parametre olarak geçebiliriz, ama mevcut yapıyı en az bozacak şekilde bu)
                    string originalPath = _selectedFilePath;
                    _selectedFilePath = absoluteStoragePath;

                    if (categoryName == "Oliz Kampanya")
                    {
                        ProcessOlizExcel(context, newFile.Id, worksheetName);
                    }
                    else if (categoryName == "Beyaz Eşya")
                    {
                        importedCount = ProcessWhiteGoodsExcel(context, newFile.Id, whiteGoodsType);
                    }
                    else if (categoryName == "Kea")
                    {
                        importedCount = ProcessKeaExcel(context, newFile.Id, whiteGoodsType);
                    }
                    // İşlem bitti, yolu geri alalım (temizlik için gerekirse)
                    _selectedFilePath = originalPath;
                });

                if (categoryName == "Beyaz Eşya" || categoryName == "Kea")
                    await ModernDialogService.ShowAsync("Başarılı", $"✅ {whiteGoodsType}: {importedCount} ürün başarıyla kaydedildi.", ModernDialogType.Success);
                else if (categoryName == "Oliz Kampanya")
                    await ModernDialogService.ShowAsync("Başarılı", "✅ Oliz Kampanya verileri başarıyla yüklendi.", ModernDialogType.Success);
                else
                    MainSnackbar.MessageQueue?.Enqueue($"{categoryName} dosyası yüklendi.");
            }
            catch (Exception ex)
            {
                await ModernDialogService.ShowAsync("İşlem Hatası", $"Yükleme sırasında bir hata oluştu:\n{ex.Message}", ModernDialogType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                BtnUpload.IsEnabled       = true;

                // Başarılı yüklemeden sonra formu temizle
                ResetFileSelection();
            }
        }

        // ─── Form Temizleme ────────────────────────────────────────────────────────────
        private void ResetFileSelection()
        {
            _selectedFilePath    = string.Empty;
            TxtSelectedFile.Text = "";
        }

        // ─── Beyaz Eşya Excel İşleme ──────────────────────────────────────────────────
        private int ProcessWhiteGoodsExcel(AppDbContext context, int fileId, string whiteGoodsType)
        {
            // 1. Profile bul
            var profile = ColumnMappingRegistry.GetProfile(whiteGoodsType);
            if (profile == null)
                throw new InvalidOperationException($"'{whiteGoodsType}' için kolon profili bulunamadı.");

            using var package = new ExcelPackage(new FileInfo(_selectedFilePath));

            // Tüm sayfalarda arama yap — bazı dosyalarda veri ilk sayfada olmayabilir
            ExcelWorksheet? worksheet = null;
            foreach (var ws in package.Workbook.Worksheets)
            {
                // İlk boyutlu (veri içeren) sayfayı al
                if (ws.Dimension != null)
                {
                    worksheet = ws;
                    break;
                }
            }

            if (worksheet == null)
                throw new InvalidOperationException("Excel dosyasında veri içeren bir çalışma sayfası bulunamadı.");

            // DEBUG: Header satırındaki gerçek başlıkları yakala (0 kayıt gelirse tanılama için)
            var foundHeaders = new System.Text.StringBuilder();
            int colCount = worksheet.Dimension?.Columns ?? 0;
            for (int c = 1; c <= colCount; c++)
            {
                string h = worksheet.Cells[profile.HeaderRow, c].Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(h))
                    foundHeaders.Append($"[{c}:{h}] ");
            }
            string headersDebug = foundHeaders.ToString();

            var processor = new WhiteGoodsExcelProcessor();

            // 2. Processor'ı çalıştır
            var products = processor.Process(worksheet, profile, fileId);

            if (products.Count == 0)
            {
                // Hiç kayıt gelmedi — başlık eşleşmesi olmayabilir
                throw new InvalidOperationException(
                    $"Sayfa '{worksheet.Name}' için hiç kayıt okunamadı. " +
                    $"Excel başlıkları: {headersDebug}" );
            }

            // 3. Veritabanına kaydet
            context.WhiteGoodsProducts.AddRange(products);
            context.SaveChanges();

            return products.Count;
        }

        // ─── KEA Excel İşleme ─────────────────────────────────────────────────────────
        private int ProcessKeaExcel(AppDbContext context, int fileId, string keaType)
        {
            var profile = ColumnMappingRegistry.GetProfile(keaType);
            if (profile == null)
                throw new InvalidOperationException($"'{keaType}' için kolon profili bulunamadı.");

            using var package = new ExcelPackage(new FileInfo(_selectedFilePath));
            ExcelWorksheet? worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Dimension != null);

            if (worksheet == null)
                throw new InvalidOperationException("Excel dosyasında veri içeren bir çalışma sayfası bulunamadı.");

            var processor = new KeaExcelProcessor();
            var products = processor.Process(worksheet, profile, fileId);

            if (products.Count == 0)
                throw new InvalidOperationException($"'{worksheet.Name}' sayfasından veri okunamadı.");

            context.KeaProducts.AddRange(products);
            context.SaveChanges();

            return products.Count;
        }

        // ─── Oliz Kampanya Excel İşleme ───────────────────────────────────────────────
        private void ProcessOlizExcel(AppDbContext context, int fileId, string sheetName)
        {
            using var package = new ExcelPackage(new FileInfo(_selectedFilePath));
            var worksheet     = package.Workbook.Worksheets[sheetName];
            if (worksheet == null) return;

            int rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++)
            {
                var campaign = new OlizCampaign
                {
                    Brand                  = worksheet.Cells[row, 1].Text,
                    ProductGroup           = worksheet.Cells[row, 2].Text,
                    ProductCode            = worksheet.Cells[row, 3].Text,
                    ProductDescription     = worksheet.Cells[row, 4].Text,
                    DiscountAmount         = ParseDecimalFromCell(worksheet.Cells[row, 5]),
                    DiscountNetAmount      = ParseDecimalFromCell(worksheet.Cells[row, 6]),
                    CampaignStartDate      = ParseDateFromCell(worksheet.Cells[row, 7]),
                    CampaignEndDate        = ParseDateFromCell(worksheet.Cells[row, 8]),
                    LastTransportDate      = ParseDateFromCell(worksheet.Cells[row, 9]),
                    LastBarcodeScanDate    = ParseDateFromCell(worksheet.Cells[row, 10]),
                    CampaignCode           = worksheet.Cells[row, 11].Text,
                    CampaignShortDescription = worksheet.Cells[row, 12].Text,
                    GeneralDescription     = worksheet.Cells[row, 13].Text,
                    UploadedFileId         = fileId
                };

                if (string.IsNullOrWhiteSpace(campaign.ProductCode) &&
                    string.IsNullOrWhiteSpace(campaign.CampaignCode)) continue;

                context.OlizCampaigns.Add(campaign);
            }
            context.SaveChanges();
        }

        // ─── Yardımcı Parser'lar (Oliz için) ──────────────────────────────────────────
        private decimal ParseDecimalFromCell(ExcelRange cell)
        {
            if (cell.Value == null) return 0;
            if (cell.Value is double d)  return (decimal)d;
            if (cell.Value is decimal m) return m;
            if (cell.Value is int i)     return (decimal)i;

            string text = (cell.Text ?? string.Empty)
                .ToUpper().Replace("TL", "").Replace(" ", "").Trim();

            return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                new System.Globalization.CultureInfo("tr-TR"), out decimal result)
                ? result : 0;
        }

        private string ParseDateFromCell(ExcelRange cell)
        {
            if (cell.Value == null) return string.Empty;

            DateTime parsedDt = DateTime.MinValue;

            if (cell.Value is DateTime dt)        parsedDt = dt;
            else if (cell.Value is double dbl)    parsedDt = DateTime.FromOADate(dbl);
            else
            {
                string text = cell.Text ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                    DateTime.TryParse(text, new System.Globalization.CultureInfo("tr-TR"),
                        System.Globalization.DateTimeStyles.None, out parsedDt);
            }

            return parsedDt == DateTime.MinValue ? string.Empty : parsedDt.ToString("dd.MM.yyyy");
        }
    }
}
