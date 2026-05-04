using ArcelikApp.Data;
using ArcelikApp.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MaterialDesignThemes.Wpf;

namespace ArcelikExcelApp.Views
{
    public partial class SettingsView : UserControl
    {
        private List<string> _tablesPendingReset = new List<string>();

        public SettingsView()
        {
            InitializeComponent();
            if (AuthService.CurrentUser != null)
            {
                TxtProfileName.Text = AuthService.CurrentUser.Username;
                TxtProfileRole.Text = AuthService.CurrentUser.Role + " Yetkisi";
                TxtDeviceId.Text = string.IsNullOrEmpty(AuthService.CurrentUser.DeviceId) ? "Cihaz Kaydı Bulunmuyor" : AuthService.CurrentUser.DeviceId;

                // Yetki Kontrolü: Sadece Admin veri sıfırlama panelini görebilir
                if (AuthService.CurrentUser.Role == "Admin")
                {
                    CardDataManagement.Visibility = Visibility.Visible;
                    CardAgreementManagement.Visibility = Visibility.Visible;

                    // Mevcut sözleşmeyi yükle
                    LoadCurrentAgreement();
                }
                else
                {
                    CardDataManagement.Visibility = Visibility.Collapsed;
                    CardAgreementManagement.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Kullanıcı oturumu yoksa (güvenlik için) paneli gizle
                CardDataManagement.Visibility = Visibility.Collapsed;
                CardAgreementManagement.Visibility = Visibility.Collapsed;
            }
        }

        private async void LoadCurrentAgreement()
        {
            var latest = await Task.Run(() => AuthService.GetLatestAgreement());
            if (latest != null)
            {
                TxtAgreementVersion.Text = latest.Version;
                TxtAgreementContent.Text = latest.Content;
            }
        }

        private async void BtnSaveAgreement_Click(object sender, RoutedEventArgs e)
        {
            string version = TxtAgreementVersion.Text.Trim();
            string content = TxtAgreementContent.Text.Trim();

            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(content))
            {
                ShowError("Versiyon ve içerik boş bırakılamaz.");
                return;
            }

            BtnSaveAgreement.IsEnabled = false;

            bool success = await Task.Run(() => AuthService.SaveNewAgreement(version, content));

            BtnSaveAgreement.IsEnabled = true;

            if (success)
            {
                ShowToast("Yeni sözleşme başarıyla yayınlandı. Tüm kullanıcılar bir sonraki girişlerinde onaylamak zorunda kalacak.");
            }
            else
            {
                ShowError("Sözleşme kaydedilirken bir hata oluştu.");
            }
        }

        private async void BtnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            string current = TxtCurrentPassword.Password;
            string newPass = TxtNewPassword.Password;
            string confirm = TxtConfirmPassword.Password;

            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirm))
            {
                ShowError("Lütfen tüm alanları doldurun.");
                return;
            }

            if (newPass != confirm)
            {
                ShowError("Yeni şifreler uyuşmuyor.");
                return;
            }

            if (AuthService.CurrentUser == null) return;

            if (!SecurityHelper.VerifyPassword(current, AuthService.CurrentUser.PasswordHash))
            {
                ShowError("Mevcut şifreniz hatalı.");
                return;
            }

            try
            {
                int userId = AuthService.CurrentUser.Id;
                string newHash = SecurityHelper.HashPassword(newPass);
                
                await System.Threading.Tasks.Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    var user = db.Users.Find(userId);
                    if (user != null)
                    {
                        user.PasswordHash = newHash;
                        db.SaveChanges();
                    }
                });
                
                // Update current user hash as well
                AuthService.CurrentUser.PasswordHash = newHash;
                
                ShowToast("Şifreniz başarıyla güncellendi.");
                TxtCurrentPassword.Password = "";
                TxtNewPassword.Password = "";
                TxtConfirmPassword.Password = "";
                TxtSettingsError.Visibility = Visibility.Collapsed;
            }
            catch (System.Exception ex)
            {
                ShowError($"Hata: {ex.Message}");
            }
        }

        private async void ShowToast(string message)
        {
            TxtToastMessage.Text = message;
            ToastCard.Visibility = Visibility.Visible;

            // Fade and Slide In
            var fadeIn = new DoubleAnimation(1, System.TimeSpan.FromMilliseconds(400));
            var slideIn = new DoubleAnimation(0, System.TimeSpan.FromMilliseconds(400));
            
            ToastCard.BeginAnimation(OpacityProperty, fadeIn);
            ((TranslateTransform)ToastCard.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideIn);

            await Task.Delay(4000);
            
            if (ToastCard.Visibility == Visibility.Visible)
                HideToast();
        }

        private void BtnCloseToast_Click(object sender, RoutedEventArgs e)
        {
            HideToast();
        }

        private void HideToast()
        {
            // Fade and Slide Out
            var fadeOut = new DoubleAnimation(0, System.TimeSpan.FromMilliseconds(400));
            var slideOut = new DoubleAnimation(50, System.TimeSpan.FromMilliseconds(400));

            fadeOut.Completed += (s, e) => ToastCard.Visibility = Visibility.Collapsed;
            
            ToastCard.BeginAnimation(OpacityProperty, fadeOut);
            ((TranslateTransform)ToastCard.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideOut);
        }

        private void ShowError(string message)
        {
            TxtSettingsError.Text = message;
            TxtSettingsError.Visibility = Visibility.Visible;
        }

        private void ChkSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(true);
        }

        private void ChkSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(false);
        }

        private void SetAllCheckboxes(bool isChecked)
        {
            ChkCostCalculations.IsChecked = isChecked;
            ChkOlizCampaigns.IsChecked = isChecked;
            ChkKeaProducts.IsChecked = isChecked;
            ChkWhiteGoodsProducts.IsChecked = isChecked;
        }

        private async void BtnResetTables_Click(object sender, RoutedEventArgs e)
        {
            // Güvenlik Kontrolü: Kod seviyesinde yetki doğrulaması
            if (AuthService.CurrentUser?.Role != "Admin")
            {
                ShowModernAlert("Yetki Hatası", "Bu işlem için yönetici yetkisi gereklidir. Lütfen sistem yöneticisiyle iletişime geçin.", false);
                return;
            }

            var tablesToReset = new List<string>();
            if (ChkCostCalculations.IsChecked == true) tablesToReset.Add("CostCalculations");
            if (ChkOlizCampaigns.IsChecked == true) tablesToReset.Add("OlizCampaigns");
            if (ChkKeaProducts.IsChecked == true) tablesToReset.Add("KeaProducts");
            if (ChkWhiteGoodsProducts.IsChecked == true) tablesToReset.Add("WhiteGoodsProducts");

            if (tablesToReset.Count == 0)
            {
                ShowModernAlert("Uyarı", "Lütfen sıfırlanacak en az bir tablo seçin.", false);
                return;
            }

            _tablesPendingReset = tablesToReset;
            ShowModernAlert("Tablo Sıfırlama Onayı", $"{string.Join(", ", tablesToReset)} tabloları tamamen boşaltılacak ve ID'ler sıfırlanacak. Bu işlem geri alınamaz.\n\nDevam etmek istiyor musunuz?", true);
        }

        private void ShowModernAlert(string title, string message, bool isConfirmation)
        {
            TxtAlertTitle.Text = title;
            TxtAlertMessage.Text = message;

            if (isConfirmation)
            {
                BtnCancelAlert.Visibility = Visibility.Visible;
                BtnConfirmAction.Content = "Evet, Sıfırla";
                BtnConfirmAction.Background = (Brush)new BrushConverter().ConvertFrom("#E02020");
                BtnConfirmAction.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#E02020");
                
                IconAlert.Kind = PackIconKind.AlertCircleOutline;
                BorderAlertIcon.Background = (Brush)new BrushConverter().ConvertFrom("#FFF3E0");
                IconAlert.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF9800");
            }
            else
            {
                BtnCancelAlert.Visibility = Visibility.Collapsed;
                BtnConfirmAction.Content = "Anladım";
                BtnConfirmAction.Background = (Brush)new BrushConverter().ConvertFrom("#1A1A24");
                BtnConfirmAction.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#1A1A24");

                IconAlert.Kind = PackIconKind.InformationOutline;
                BorderAlertIcon.Background = (Brush)new BrushConverter().ConvertFrom("#E3F2FD");
                IconAlert.Foreground = (Brush)new BrushConverter().ConvertFrom("#2196F3");
            }

            AlertOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelAlert_Click(object sender, RoutedEventArgs e)
        {
            AlertOverlay.Visibility = Visibility.Collapsed;
            _tablesPendingReset.Clear();
        }

        private async void BtnConfirmAlert_Click(object sender, RoutedEventArgs e)
        {
            AlertOverlay.Visibility = Visibility.Collapsed;
            
            try
            {
                using (var db = new AppDbContext())
                {
                    foreach (var tableName in _tablesPendingReset)
                    {
                        // Güvenlik: Sadece izin verilen tablo isimlerini çalıştır
                        string? sql = tableName switch
                        {
                            "CostCalculations"     => "TRUNCATE TABLE CostCalculations",
                            "OlizCampaigns"       => "TRUNCATE TABLE OlizCampaigns",
                            "KeaProducts"         => "TRUNCATE TABLE KeaProducts",
                            "WhiteGoodsProducts"  => "TRUNCATE TABLE WhiteGoodsProducts",
                            _ => null
                        };

                        if (sql != null)
                        {
                            await db.Database.ExecuteSqlRawAsync(sql);
                        }
                    }
                }
                ShowToast("Seçili tablolar başarıyla sıfırlandı.");
                SetAllCheckboxes(false);
                ChkSelectAll.IsChecked = false;
            }
            catch (System.Exception ex)
            {
                ShowModernAlert("Hata Oluştu", $"Sıfırlama sırasında teknik bir hata oluştu: {ex.Message}", false);
            }
            finally
            {
                _tablesPendingReset.Clear();
            }
        }
    }
}
