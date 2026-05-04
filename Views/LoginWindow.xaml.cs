using ArcelikApp.Data;
using ArcelikApp.Services;
using ArcelikApp.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ArcelikExcelApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            
            // Modern Dialog Servisine abone ol
            ModernDialogService.DialogRequested += ModernDialogService_DialogRequested;

            this.Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await TryConnectAndInitialize();
        }

        /// <summary>
        /// Veritabanı bağlantısını retry ile dener, başarılı olursa giriş ekranını gösterir.
        /// </summary>
        private async Task TryConnectAndInitialize()
        {
            // Bağlantı denenirken UI'ı göster ve durumu bildir
            this.Visibility = Visibility.Visible;
            ShowConnectionStatus("Veritabanı sunucusuna bağlanılıyor...");

            // Başlangıçta kısa bir gecikme verelim ki sistem hazırlansın
            await System.Threading.Tasks.Task.Delay(500);

            // 3 deneme ile bağlantıyı test et (exponential backoff: 2s, 4s)
            bool isDbAvailable = await AppDbContext.TestConnectionWithRetryAsync(maxRetries: 3);

            HideConnectionStatus();

            if (!isDbAvailable)
            {
                // Bağlantı başarısız — "Tekrar Dene" butonlu dialog göster
                ShowRetryDialog(
                    "Sunucu Bağlantı Hatası",
                    "Veritabanı sunucusuna 3 deneme sonrası bağlanılamadı.\n\n" +
                    "Olası nedenler:\n" +
                    "• Ana bilgisayar kapalı olabilir\n" +
                    "• Ağ bağlantınızda sorun olabilir\n" +
                    "• MySQL servisi çalışmıyor olabilir\n\n" +
                    "Tekrar denemek için aşağıdaki butona basın.");
                return;
            }

            // Bağlantı varsa işlemlere devam et
            await Task.Run(() => AuthService.CreateInitialAdmin()); 

            // Artık otomatik giriş kontrolünü App.xaml.cs yapıyor
            // Bu pencere açıldıysa zaten manuel giriş gerekiyordur
            BtnLogin.IsEnabled = true;
        }

        private void ShowConnectionStatus(string message)
        {
            BtnLogin.IsEnabled = false;
            TxtError.Text = message;
            TxtError.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            TxtError.Visibility = Visibility.Visible;
        }

        private void HideConnectionStatus()
        {
            TxtError.Visibility = Visibility.Collapsed;
            TxtError.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
        }

        /// <summary>
        /// Tekrar Dene butonlu bağlantı hatası dialogu gösterir.
        /// </summary>
        private void ShowRetryDialog(string title, string message)
        {
            TxtDialogTitle.Text = title;
            TxtDialogMessage.Text = message;

            // Hata teması
            BorderDialogIcon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
            IconDialog.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            IconDialog.Kind = MaterialDesignThemes.Wpf.PackIconKind.ServerNetworkOff;

            BtnDialogConfirm.Content = "Tekrar Dene";
            BtnDialogConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            BtnDialogConfirm.BorderBrush = BtnDialogConfirm.Background;

            // Tekrar Dene butonuna özel tag atıyoruz
            BtnDialogConfirm.Tag = "RetryConnection";

            ModernDialogOverlay.Visibility = Visibility.Visible;
        }

        #region Modern Dialog System
        private void ModernDialogService_DialogRequested(object? sender, ModernDialogEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TxtDialogTitle.Text = e.Title;
                TxtDialogMessage.Text = e.Message;
                
                // Tema ayarları
                switch (e.Type)
                {
                    case ModernDialogType.Error:
                        BorderDialogIcon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                        IconDialog.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                        IconDialog.Kind = MaterialDesignThemes.Wpf.PackIconKind.AlertOctagon;
                        BtnDialogConfirm.Background = IconDialog.Foreground;
                        BtnDialogConfirm.BorderBrush = IconDialog.Foreground;
                        break;
                    default:
                        BorderDialogIcon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"));
                        IconDialog.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                        IconDialog.Kind = MaterialDesignThemes.Wpf.PackIconKind.Information;
                        BtnDialogConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A24"));
                        BtnDialogConfirm.BorderBrush = BtnDialogConfirm.Background;
                        break;
                }

                ModernDialogOverlay.Visibility = Visibility.Visible;
            });
        }

        private void BtnDialogResult_Click(object sender, RoutedEventArgs e)
        {
            ModernDialogOverlay.Visibility = Visibility.Collapsed;
            
            // "Tekrar Dene" butonuna basıldıysa bağlantıyı tekrar dene
            if (BtnDialogConfirm.Tag as string == "RetryConnection")
            {
                BtnDialogConfirm.Tag = null;
                BtnDialogConfirm.Content = "Tamam";
                _ = TryConnectAndInitialize();
                return;
            }
            
            ModernDialogService.SetResult(true);
        }
        #endregion

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPasswordVisible.IsVisible ? TxtPasswordVisible.Text : TxtPassword.Password;
            string license = TxtLicenseKey.Text.Trim();
            bool remember = ChkRememberMe.IsChecked ?? false;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Kullanıcı adı ve şifre boş bırakılamaz.");
                return;
            }

            BtnLogin.IsEnabled = false;
            TxtError.Visibility = Visibility.Collapsed;

            var result = await Task.Run(() => AuthService.Login(username, password, license, remember));

            BtnLogin.IsEnabled = true;

            if (result.Success && result.User != null)
            {
                // Başarılı giriş
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
            else if (result.NeedsAgreementAcceptance && result.User != null)
            {
                // Yeni sözleşme onayı gerekiyor
                _tempUserForAgreement = result.User;
                _currentAgreementId = result.LatestAgreementId ?? 0;
                _isEnforcedAgreement = true;
                
                LnkAgreement_Click(null, null); // Sözleşmeyi aç
                ShowError(result.Message);
            }
            else
            {
                if (result.NeedsActivation)
                {
                    TxtLicenseKey.Visibility = Visibility.Visible;
                }
                ShowError(result.Message);
            }
        }

        private User? _tempUserForAgreement;
        private bool _isEnforcedAgreement = false;

        private void LnkForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ResetOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelReset_Click(object sender, RoutedEventArgs e)
        {
            ResetOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnConfirmReset_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtResetUsername.Text.Trim();
            string license = TxtResetLicense.Text.Trim();
            string newPass = TxtResetNewPasswordVisible.IsVisible ? TxtResetNewPasswordVisible.Text : TxtResetNewPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(license) || string.IsNullOrEmpty(newPass))
            {
                ShowResetError("Lütfen tüm alanları doldurun.");
                return;
            }

            BtnConfirmReset.IsEnabled = false;
            
            bool success = await Task.Run(() => AuthService.ResetPassword(username, license, newPass));
            
            BtnConfirmReset.IsEnabled = true;

            if (success)
            {
                ShowToast("Şifreniz başarıyla sıfırlandı.");
                ResetOverlay.Visibility = Visibility.Collapsed;
                TxtResetError.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowResetError("Kullanıcı adı veya lisans anahtarı hatalı.");
            }
        }

        private async void ShowToast(string message)
        {
            TxtToastMessage.Text = message;
            ToastCard.Visibility = Visibility.Visible;

            // Fade and Slide In
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(1, System.TimeSpan.FromMilliseconds(400));
            var slideIn = new System.Windows.Media.Animation.DoubleAnimation(0, System.TimeSpan.FromMilliseconds(400));
            
            ToastCard.BeginAnimation(OpacityProperty, fadeIn);
            ((TranslateTransform)ToastCard.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideIn);

            await System.Threading.Tasks.Task.Delay(3000);

            // Fade and Slide Out
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(0, System.TimeSpan.FromMilliseconds(400));
            var slideOut = new System.Windows.Media.Animation.DoubleAnimation(50, System.TimeSpan.FromMilliseconds(400));

            fadeOut.Completed += (s, e) => ToastCard.Visibility = Visibility.Collapsed;
            
            ToastCard.BeginAnimation(OpacityProperty, fadeOut);
            ((TranslateTransform)ToastCard.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideOut);
        }

        private void ShowResetError(string message)
        {
            TxtResetError.Text = message;
            TxtResetError.Visibility = Visibility.Visible;
        }

        // --- Login Password Show/Hide ---
        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPassword.Visibility == Visibility.Visible)
            {
                TxtPasswordVisible.Text = TxtPassword.Password;
                TxtPassword.Visibility = Visibility.Collapsed;
                TxtPasswordVisible.Visibility = Visibility.Visible;
                IconShowPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
                IconShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E02020"));
                TxtPasswordVisible.Focus();
                TxtPasswordVisible.CaretIndex = TxtPasswordVisible.Text.Length;
            }
            else
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                IconShowPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
                IconShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtPassword.Focus();
            }
        }

        // --- Registration Logic ---
        private void BtnRegShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtRegPassword.Visibility == Visibility.Visible)
            {
                TxtRegPasswordVisible.Text = TxtRegPassword.Password;
                TxtRegPassword.Visibility = Visibility.Collapsed;
                TxtRegPasswordVisible.Visibility = Visibility.Visible;
                IconRegShowPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
                IconRegShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E02020"));
                TxtRegPasswordVisible.Focus();
                TxtRegPasswordVisible.CaretIndex = TxtRegPasswordVisible.Text.Length;
            }
            else
            {
                TxtRegPassword.Password = TxtRegPasswordVisible.Text;
                TxtRegPasswordVisible.Visibility = Visibility.Collapsed;
                TxtRegPassword.Visibility = Visibility.Visible;
                IconRegShowPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
                IconRegShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtRegPassword.Focus();
            }
        }

        private void BtnRegShowPasswordConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (TxtRegPasswordConfirm.Visibility == Visibility.Visible)
            {
                TxtRegPasswordConfirmVisible.Text = TxtRegPasswordConfirm.Password;
                TxtRegPasswordConfirm.Visibility = Visibility.Collapsed;
                TxtRegPasswordConfirmVisible.Visibility = Visibility.Visible;
                IconRegShowPasswordConfirm.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
                IconRegShowPasswordConfirm.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E02020"));
                TxtRegPasswordConfirmVisible.Focus();
                TxtRegPasswordConfirmVisible.CaretIndex = TxtRegPasswordConfirmVisible.Text.Length;
            }
            else
            {
                TxtRegPasswordConfirm.Password = TxtRegPasswordConfirmVisible.Text;
                TxtRegPasswordConfirmVisible.Visibility = Visibility.Collapsed;
                TxtRegPasswordConfirm.Visibility = Visibility.Visible;
                IconRegShowPasswordConfirm.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
                IconRegShowPasswordConfirm.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtRegPasswordConfirm.Focus();
            }
        }

        private int _currentAgreementId = 0;

        private async void LnkAgreement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                var agreement = db.Agreements.OrderByDescending(a => a.Id).FirstOrDefault();
                
                if (agreement != null)
                {
                    _currentAgreementId = agreement.Id;
                    TxtAgreementVersion.Text = $"Kullanıcı Sözleşmesi ({agreement.Version})";
                    TxtAgreementContent.Text = agreement.Content;
                    AgreementOverlay.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowRegError("Kullanıcı sözleşmesi sistemde bulunamadı.");
                }
            }
            catch (System.Exception ex)
            {
                ShowRegError($"Sözleşme yüklenirken hata: {ex.Message}");
            }
        }

        private void BtnCloseAgreement_Click(object sender, RoutedEventArgs e)
        {
            if (_isEnforcedAgreement)
            {
                // Onay verilmediği için çıkış yap
                Application.Current.Shutdown();
                return;
            }
            AgreementOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnAcceptAgreement_Click(object sender, RoutedEventArgs e)
        {
            if (_isEnforcedAgreement && _tempUserForAgreement != null)
            {
                // Giriş akışında zorunlu onay
                await Task.Run(() => AuthService.AcceptAgreement(_tempUserForAgreement.Id, _currentAgreementId));
                
                // Onay alındıktan sonra devam et
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
                return;
            }

            ChkAgreement.IsChecked = true;
            AgreementOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtRegUsername.Text.Trim();
            string dealerName = TxtRegDealerName.Text.Trim();
            
            string password = TxtRegPasswordVisible.IsVisible ? TxtRegPasswordVisible.Text : TxtRegPassword.Password;
            string passwordConfirm = TxtRegPasswordConfirmVisible.IsVisible ? TxtRegPasswordConfirmVisible.Text : TxtRegPasswordConfirm.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(dealerName) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordConfirm))
            {
                ShowRegError("Lütfen tüm alanları doldurun.");
                return;
            }

            if (password != passwordConfirm)
            {
                ShowRegError("Şifreler uyuşmuyor.");
                return;
            }

            if (password.Length < 6)
            {
                ShowRegError("Şifre en az 6 karakter olmalıdır.");
                return;
            }

            if (ChkAgreement.IsChecked != true || _currentAgreementId == 0)
            {
                ShowRegError("Kayıt olmak için kullanıcı sözleşmesini okuyup kabul etmelisiniz.");
                return;
            }

            BtnRegister.IsEnabled = false;
            TxtRegError.Visibility = Visibility.Collapsed;

            var result = await Task.Run(() => AuthService.Register(username, dealerName, password, _currentAgreementId));

            BtnRegister.IsEnabled = true;

            if (result.Success)
            {
                ShowToast("Kayıt başarılı! Giriş yaparak lisansınızı etkinleştirebilirsiniz.");
                TxtRegUsername.Text = "";
                TxtRegDealerName.Text = "";
                TxtRegPassword.Password = "";
                TxtRegPasswordVisible.Text = "";
                TxtRegPasswordConfirm.Password = "";
                TxtRegPasswordConfirmVisible.Text = "";
                ChkAgreement.IsChecked = false;
            }
            else
            {
                ShowRegError(result.Message);
            }
        }

        private void ShowRegError(string message)
        {
            TxtRegError.Text = message;
            TxtRegError.Visibility = Visibility.Visible;
        }

        private void BtnSwitchToRegister_Click(object sender, RoutedEventArgs e)
        {
            ViewLogin.Visibility = Visibility.Collapsed;
            ViewRegister.Visibility = Visibility.Visible;
            TxtRegError.Visibility = Visibility.Collapsed;
        }

        private void BtnSwitchToLogin_Click(object sender, RoutedEventArgs e)
        {
            ViewRegister.Visibility = Visibility.Collapsed;
            ViewLogin.Visibility = Visibility.Visible;
            TxtError.Visibility = Visibility.Collapsed;
        }

        // --- Reset Password Show/Hide ---
        private void BtnShowResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtResetNewPassword.Visibility == Visibility.Visible)
            {
                TxtResetNewPasswordVisible.Text = TxtResetNewPassword.Password;
                TxtResetNewPassword.Visibility = Visibility.Collapsed;
                TxtResetNewPasswordVisible.Visibility = Visibility.Visible;
                IconShowResetPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
                IconShowResetPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E02020"));
                TxtResetNewPasswordVisible.Focus();
                TxtResetNewPasswordVisible.CaretIndex = TxtResetNewPasswordVisible.Text.Length;
            }
            else
            {
                TxtResetNewPassword.Password = TxtResetNewPasswordVisible.Text;
                TxtResetNewPasswordVisible.Visibility = Visibility.Collapsed;
                TxtResetNewPassword.Visibility = Visibility.Visible;
                IconShowResetPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
                IconShowResetPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtResetNewPassword.Focus();
            }
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
