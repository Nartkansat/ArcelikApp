using ArcelikApp.Data;
using ArcelikApp.Models;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ArcelikApp.Services;


namespace ArcelikExcelApp.Views
{
    public partial class ExcelViewer : UserControl
    {
        private List<UploadedFile> _allFiles = new();

        public ExcelViewer()
        {
            InitializeComponent();
            ExcelPackage.License.SetNonCommercialPersonal("NART");
            LoadFilesFromDb();
        }

        public void LoadSpecificFile(int fileId)
        {
            var file = _allFiles.FirstOrDefault(x => x.Id == fileId);
            if (file != null)
            {
                ListFiles.SelectedItem = file;
            }
        }

        private void LoadFilesFromDb()
        {
            try
            {
                using var db = new AppDbContext();
                _allFiles = db.UploadedFiles.OrderByDescending(x => x.Id).ToList();
                ListFiles.ItemsSource = _allFiles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosyalar yüklenirken hata oluştu: {ex.Message}");
            }
        }

        private void TxtSearchFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtSearchFile.Text.ToLower();
            ListFiles.ItemsSource = string.IsNullOrWhiteSpace(query)
                ? _allFiles
                : _allFiles.Where(x => x.FileName.ToLower().Contains(query)).ToList();
        }

        private async void ListFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListFiles.SelectedItem is UploadedFile selectedFile)
            {
                // 1. Önce Veritabanındaki veriyi kontrol et (En dinamik yöntem)
                if (selectedFile.FileData != null && selectedFile.FileData.Length > 0)
                {
                    await LoadExcelFileAsync(selectedFile.FileData, selectedFile.FileName);
                    return;
                }

                // 2. Eğer DB'de yoksa (eski kayıtlar), yerel dosyaya bak
                string absolutePath = FileHelper.GetAbsolutePath(selectedFile.FilePath);
                
                if (File.Exists(absolutePath))
                {
                    await LoadExcelFileAsync(await File.ReadAllBytesAsync(absolutePath), selectedFile.FileName);
                }
                else
                {
                    MessageBox.Show($"Dosya ne veritabanında ne de yerel diskte bulunamadı:\n{absolutePath}", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string file = files.FirstOrDefault(x => x.EndsWith(".xlsx") || x.EndsWith(".xls"));
                if (!string.IsNullOrEmpty(file))
                {
                    await LoadExcelFileAsync(await File.ReadAllBytesAsync(file), Path.GetFileName(file));
                    ListFiles.SelectedItem = null; // Unselect DB file
                }
            }
        }

        private async void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Dosyaları|*.xlsx;*.xls",
                Title = "Görüntülenecek Excel Dosyasını Seçin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadExcelFileAsync(await File.ReadAllBytesAsync(openFileDialog.FileName), Path.GetFileName(openFileDialog.FileName));
                ListFiles.SelectedItem = null; // Unselect DB file
            }
        }

        private async Task LoadExcelFileAsync(byte[] fileData, string fileName)
        {
            OverlayLoading.Visibility = Visibility.Visible;
            PnlEmptyState.Visibility = Visibility.Collapsed;
            TabWorksheets.Items.Clear();

            try
            {
                using var memoryStream = new MemoryStream(fileData);
                using var package = new ExcelPackage(memoryStream);

                // Process each worksheet
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Dimension == null) continue; // Empty worksheet

                    string html = await Task.Run(() => GenerateHtmlForWorksheet(ws));

                    // Create Tab
                    var tabItem = new TabItem
                    {
                        Header = ws.Name,
                        Background = Brushes.Transparent
                    };

                    // Create WebBrowser
                    var webBrowser = new WebBrowser();
                    webBrowser.NavigateToString(html);

                    tabItem.Content = webBrowser;
                    TabWorksheets.Items.Add(tabItem);
                }

                if (TabWorksheets.Items.Count > 0)
                {
                    TabWorksheets.SelectedIndex = 0;
                    PnlExcelToolbar.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel dosyası okunurken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                PnlEmptyState.Visibility = Visibility.Visible;
            }
            finally
            {
                OverlayLoading.Visibility = Visibility.Collapsed;
            }
        }

        private string GenerateHtmlForWorksheet(ExcelWorksheet ws)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><meta http-equiv='X-UA-Compatible' content='IE=edge'><style>");
            sb.Append("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; font-size: 13px; margin: 0; padding: 10px; background-color: #f8fafc; } ");
            sb.Append("table { border-collapse: collapse; background-color: white; box-shadow: 0 1px 3px rgba(0,0,0,0.1); } ");
            sb.Append("th, td { border: 1px solid #e2e8f0; padding: 6px 12px; white-space: nowrap; } ");
            sb.Append("tr:nth-child(even) { background-color: #f8fafc; } ");
            sb.Append(".highlight { background-color: #FFE082 !important; color: black !important; } ");
            sb.Append(".current-match { background-color: #FFD54F !important; border: 2px solid #E02020 !important; } ");
            sb.Append("</style></head><body>");
            sb.Append("<table>");

            int maxRow = Math.Min(ws.Dimension.End.Row, 1000); // Limit to 1000 rows for performance
            int maxCol = ws.Dimension.End.Column;

            for (int r = 1; r <= maxRow; r++)
            {
                sb.Append("<tr>");
                for (int c = 1; c <= maxCol; c++)
                {
                    var cell = ws.Cells[r, c];
                    string cellText = cell.Text ?? "";
                    string styleAttr = "";

                    // Check background color
                    if (cell.Style.Fill.PatternType != OfficeOpenXml.Style.ExcelFillStyle.None)
                    {
                        string bgHex = cell.Style.Fill.BackgroundColor.Rgb;
                        if (!string.IsNullOrEmpty(bgHex) && bgHex.Length == 8) // ARGB
                        {
                            bgHex = "#" + bgHex.Substring(2); // Convert to RGB
                            if (bgHex != "#000000") // Ignore default 'Auto' black
                            {
                                styleAttr += $"background-color: {bgHex}; ";
                            }
                        }
                    }

                    // Check font color
                    string fontHex = cell.Style.Font.Color.Rgb;
                    if (!string.IsNullOrEmpty(fontHex) && fontHex.Length == 8)
                    {
                        fontHex = "#" + fontHex.Substring(2);
                        if (fontHex != "#000000") // Ignore default 'Auto' black
                        {
                            styleAttr += $"color: {fontHex}; ";
                        }
                    }

                    // Bold
                    if (cell.Style.Font.Bold)
                    {
                        styleAttr += "font-weight: bold; ";
                    }

                    string tag = r == 1 ? "th" : "td";
                    if (!string.IsNullOrEmpty(styleAttr))
                    {
                        sb.Append($"<{tag} style='{styleAttr}'>{System.Net.WebUtility.HtmlEncode(cellText)}</{tag}>");
                    }
                    else
                    {
                        sb.Append($"<{tag}>{System.Net.WebUtility.HtmlEncode(cellText)}</{tag}>");
                    }
                }
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            
            if (ws.Dimension.End.Row > 1000)
            {
                sb.Append($"<p style='color: #64748b; margin-top: 10px; font-size: 11px;'>Performans nedeniyle sadece ilk 1000 satır gösterilmektedir. Toplam satır: {ws.Dimension.End.Row}</p>");
            }
            
            sb.Append(@"
<script>
var foundElements = [];
var currentIndex = -1;

function searchText(query, next) {
    query = query.toLowerCase().trim();
    
    if (!next) {
        // Clear old highlights
        for(var i=0; i<foundElements.length; i++) {
            foundElements[i].className = foundElements[i].className.replace(' highlight', '').replace(' current-match', '');
        }
        foundElements = [];
        currentIndex = -1;

        if (query === '') return;

        var tds = document.getElementsByTagName('td');
        var ths = document.getElementsByTagName('th');
        var all = [];
        for(var i=0;i<tds.length;i++) all.push(tds[i]);
        for(var i=0;i<ths.length;i++) all.push(ths[i]);

        for(var i=0; i<all.length; i++){
            if(all[i].innerText.toLowerCase().indexOf(query) !== -1){
                foundElements.push(all[i]);
                all[i].className += ' highlight';
            }
        }
    }

    if (foundElements.length > 0) {
        // Remove current match focus
        if (currentIndex >= 0 && currentIndex < foundElements.length) {
            foundElements[currentIndex].className = foundElements[currentIndex].className.replace(' current-match', '');
        }

        currentIndex = (currentIndex + 1) % foundElements.length;
        var el = foundElements[currentIndex];
        
        el.className += ' current-match';
        el.scrollIntoView({behavior: 'smooth', block: 'center'});
    }
}
</script>
");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (PnlExcelToolbar.Visibility == Visibility.Visible)
                {
                    TxtSearchExcel.Focus();
                    TxtSearchExcel.SelectAll();
                    e.Handled = true;
                }
            }
        }

        private string _lastQuery = "";

        private void TxtSearchExcel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch(TxtSearchExcel.Text == _lastQuery);
                _lastQuery = TxtSearchExcel.Text;
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch(false);
            _lastQuery = TxtSearchExcel.Text;
        }

        private void BtnNextMatch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch(true);
        }

        private void PerformSearch(bool next)
        {
            if (TabWorksheets.SelectedItem is TabItem selectedTab && selectedTab.Content is WebBrowser webBrowser)
            {
                try
                {
                    webBrowser.InvokeScript("searchText", new object[] { TxtSearchExcel.Text, next });
                }
                catch
                {
                    // Ignore script errors
                }
            }
        }
    }
}
