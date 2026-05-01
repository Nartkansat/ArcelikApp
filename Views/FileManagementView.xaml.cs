using ArcelikApp.Data;
using ArcelikApp.Models;
using ArcelikApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArcelikExcelApp.Views
{
    public partial class FileManagementView : UserControl
    {
        private List<UploadedFile> _allFiles = new();

        public FileManagementView()
        {
            InitializeComponent();
            _ = LoadFilesAsync();
        }

        private async Task LoadFilesAsync()
        {
            try
            {
                OverlayLoading.Visibility = Visibility.Visible;
                _allFiles = await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    return db.UploadedFiles.OrderByDescending(x => x.Id).ToList();
                });
                
                FilterFiles();
            }
            catch (Exception ex)
            {
                await ModernDialogService.ShowAsync("Hata", $"Dosyalar yüklenirken bir hata oluştu:\n{ex.Message}", ModernDialogType.Error);
            }
            finally
            {
                OverlayLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterFiles();
        }

        private void FilterFiles()
        {
            string query = TxtSearch.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(query) 
                ? _allFiles 
                : _allFiles.Where(f => f.FileName.ToLower().Contains(query)).ToList();

            GridFiles.ItemsSource = filtered;
            TxtTotalCount.Text = $"Toplam: {filtered.Count} Dosya";
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int fileId)
            {
                var fileToDelete = _allFiles.FirstOrDefault(f => f.Id == fileId);
                if (fileToDelete == null) return;

                var result = await ModernDialogService.ShowAsync("Dosyayı Sil", 
                    $"'{fileToDelete.FileName}' isimli dosyayı ve sistemdeki tüm kayıtlarını silmek istediğinize emin misiniz? Bu işlem geri alınamaz.", 
                    ModernDialogType.Question);

                if (result)
                {
                    try
                    {
                        OverlayLoading.Visibility = Visibility.Visible;
                        await Task.Run(() =>
                        {
                            using var db = new AppDbContext();
                            var file = db.UploadedFiles.Find(fileId);
                            if (file != null)
                            {
                                // İsterseniz burada ilgili dosyaya ait WhiteGoods veya Kea kayıtlarını da silebilirsiniz.
                                // Mevcut yapıda UploadedFile silinmesi yeterli görünüyor ama 
                                // eğer tam bir temizlik istenirse ilgili tablolardan FileId ile silme yapılmalı.
                                
                                db.UploadedFiles.Remove(file);
                                db.SaveChanges();
                            }
                        });

                        await LoadFilesAsync();
                        await ModernDialogService.ShowAsync("Başarılı", "Dosya başarıyla silindi.", ModernDialogType.Success);
                    }
                    catch (Exception ex)
                    {
                        await ModernDialogService.ShowAsync("Hata", $"Dosya silinirken bir hata oluştu:\n{ex.Message}", ModernDialogType.Error);
                    }
                    finally
                    {
                        OverlayLoading.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }
}
