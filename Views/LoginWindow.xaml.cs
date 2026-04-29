using ArcelikApp.Data;
using ArcelikApp.Services;
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
            // Veritabanı bağlantısını kontrol et
            bool isDbAvailable = false;
            
            // Başlangıçta kısa bir gecikme verelim ki sistem hazırlansın
            await System.Threading.Tasks.Task.Delay(500);

            try
            {
                await System.Threading.Tasks.Task.Run(() => {
                    isDbAvailable = AppDbContext.TestConnection();
                });
            }
            catch { }

            if (!isDbAvailable)
            {
                this.Visibility = Visibility.Visible; // Bağlantı yoksa ekranı göster ki hata mesajı görünsün
                _ = ModernDialogService.ShowAsync("Sunucu Hatası", 
                    "Veritabanı sunucusuna bağlanılamadı. Sunucu şu an kapalı olabilir veya internet bağlantınızda bir sorun olabilir.", 
                    ModernDialogType.Error);
                return;
            }

            // Bağlantı varsa işlemlere devam et
            AuthService.CreateInitialAdmin(); 

            // Artık otomatik giriş kontrolünü App.xaml.cs yapıyor
            // Bu pencere açıldıysa zaten manuel giriş gerekiyordur
            this.Visibility = Visibility.Visible;
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

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;
            string license = TxtLicenseKey.Text.Trim();
            bool remember = ChkRememberMe.IsChecked ?? false;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Kullanıcı adı ve şifre boş bırakılamaz.");
                return;
            }

            var result = AuthService.Login(username, password, license, remember);

            if (result.Success && result.User != null)
            {
                // Başarılı giriş
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
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

        private void LnkForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ResetOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelReset_Click(object sender, RoutedEventArgs e)
        {
            ResetOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirmReset_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtResetUsername.Text.Trim();
            string license = TxtResetLicense.Text.Trim();
            string newPass = TxtResetNewPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(license) || string.IsNullOrEmpty(newPass))
            {
                ShowResetError("Lütfen tüm alanları doldurun.");
                return;
            }

            if (AuthService.ResetPassword(username, license, newPass))
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

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
