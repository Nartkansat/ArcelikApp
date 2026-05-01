using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ArcelikExcelApp.Views;
using ArcelikApp.Services;

namespace ArcelikExcelApp
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _notificationPollTimer;
        private System.Collections.Generic.HashSet<int> _notifiedIds = new();
        
        // Session kontrolünü cache'le — her tıklamada DB'ye gitmesin
        private DateTime _lastSessionCheck = DateTime.MinValue;
        private bool _lastSessionValid = true;
        private static readonly TimeSpan SessionCheckInterval = TimeSpan.FromSeconds(30);

        public MainWindow()
        {
            InitializeComponent();

            // Uygulama versiyonunu göster
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                TxtAppVersion.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }

            // Giriş yapan kullanıcı bilgilerini göster
            if (AuthService.CurrentUser != null)
            {
                TxtUsername.Text = AuthService.CurrentUser.Username;
                TxtRole.Text = AuthService.CurrentUser.Role;

                // Admin değilse admin bölümünü gizle
                if (AuthService.CurrentUser.Role != "Admin")
                {
                    AdminSection.Visibility = Visibility.Collapsed;
                }
            }

            // Subscribe to notification changes
            NotificationService.NotificationsChanged += (s, e) => {
                Dispatcher.Invoke(() => _ = RefreshNotificationsAsync());
            };
            
            // Subscribe to Modern Dialog Service
            ModernDialogService.DialogRequested += ModernDialogService_DialogRequested;
            
            _ = RefreshNotificationsAsync();

            // Set default view by simulating a click on the Dashboard button
            Nav_Click(BtnDashboard, null);

            // Setup polling for new notifications (async)
            _notificationPollTimer = new DispatcherTimer();
            _notificationPollTimer.Interval = TimeSpan.FromSeconds(15);
            _notificationPollTimer.Tick += NotificationPollTimer_Tick;
            _notificationPollTimer.Start();
            
            // First run to mark existing as "already notified" so we don't spam on startup
            _ = InitNotificationCacheAsync();
        }

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Buton işaretliyse (Açık Menü)
            if (MenuToggleButton.IsChecked == true)
            {
                SidebarColumn.Width = new GridLength(280);
            }
            // Buton işaretsizse (Kapalı/Dar Menü)
            else
            {
                SidebarColumn.Width = new GridLength(85);
            }
        }

        private async Task InitNotificationCacheAsync()
        {
            if (AuthService.CurrentUser != null)
            {
                int userId = AuthService.CurrentUser.Id;
                var currentNotifications = await Task.Run(() => NotificationService.GetUserNotifications(userId));
                foreach (var n in currentNotifications)
                {
                    _notifiedIds.Add(n.Id);
                }
            }
        }

        private async void NotificationPollTimer_Tick(object? sender, EventArgs e)
        {
            if (AuthService.CurrentUser == null) return;

            try
            {
                int userId = AuthService.CurrentUser.Id;
                var notifiedIdsCopy = new System.Collections.Generic.HashSet<int>(_notifiedIds);
                
                var unread = await Task.Run(() => 
                    NotificationService.GetUserNotifications(userId)
                        .Where(n => !n.IsRead && !notifiedIdsCopy.Contains(n.Id))
                        .ToList());

                if (unread.Any())
                {
                    foreach (var n in unread)
                    {
                        _notifiedIds.Add(n.Id);
                        NotificationService.ShowToast(n.Title, n.Message, n.Type);
                    }
                    await RefreshNotificationsAsync();
                }
            }
            catch { /* Ağ hatası olursa sessizce devam et */ }
        }

        #region Modern Dialog System
        private void ModernDialogService_DialogRequested(object? sender, ModernDialogEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TxtDialogTitle.Text = e.Title;
                TxtDialogMessage.Text = e.Message;
                
                // Reset buttons
                BtnDialogCancel.Visibility = e.Type == ModernDialogType.Question ? Visibility.Visible : Visibility.Collapsed;
                BtnDialogConfirm.Content = e.Type == ModernDialogType.Question ? "Evet" : "Tamam";
                Grid.SetColumn(BtnDialogConfirm, e.Type == ModernDialogType.Question ? 1 : 0);
                Grid.SetColumnSpan(BtnDialogConfirm, e.Type == ModernDialogType.Question ? 1 : 2);

                // Set theme colors/icons
                switch (e.Type)
                {
                    case ModernDialogType.Success:
                        BorderDialogIcon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                        IconDialog.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        IconDialog.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircle;
                        BtnDialogConfirm.Background = IconDialog.Foreground;
                        BtnDialogConfirm.BorderBrush = IconDialog.Foreground;
                        break;
                    case ModernDialogType.Error:
                        BorderDialogIcon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                        IconDialog.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                        IconDialog.Kind = MaterialDesignThemes.Wpf.PackIconKind.AlertOctagon;
                        BtnDialogConfirm.Background = IconDialog.Foreground;
                        BtnDialogConfirm.BorderBrush = IconDialog.Foreground;
                        break;
                    case ModernDialogType.Warning:
                    case ModernDialogType.Question:
                        BorderDialogIcon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
                        IconDialog.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                        IconDialog.Kind = MaterialDesignThemes.Wpf.PackIconKind.Alert;
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
            if (sender is Button btn && bool.TryParse(btn.Tag?.ToString(), out bool result))
            {
                ModernDialogOverlay.Visibility = Visibility.Collapsed;
                ModernDialogService.SetResult(result);
            }
        }
        #endregion

        #region Notifications
        private async Task RefreshNotificationsAsync()
        {
            if (AuthService.CurrentUser == null) return;
            
            try
            {
                int userId = AuthService.CurrentUser.Id;
                
                var result = await Task.Run(() =>
                {
                    var list = NotificationService.GetUserNotifications(userId);
                    var unreadCount = NotificationService.GetUnreadCount(userId);
                    return (list, unreadCount);
                });

                BadgeNotifications.Badge = result.unreadCount > 0 ? (object)result.unreadCount : null;
                ItemsNotifications.ItemsSource = result.list.Take(5);
            }
            catch { /* Ağ hatası olursa UI'ı çökertme */ }
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            PopupNotifications.IsOpen = !PopupNotifications.IsOpen;
        }

        private async void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser != null)
            {
                var result = await ModernDialogService.ShowAsync("Tümünü Okundu İşaretle",
                   "Tüm bildirimleri okundu olarak işaretlemek istediğinize emin misiniz?",
                   ModernDialogType.Question);

                if (result)
                {
                    int userId = AuthService.CurrentUser.Id;
                    await Task.Run(() => NotificationService.MarkAllAsRead(userId));
                    _ = ModernDialogService.ShowAsync("Başarılı", "Tüm bildirimler okundu olarak işaretlendi.", ModernDialogType.Success);
                }
            }
        }

        private void BtnNotificationDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                _ = Task.Run(() => NotificationService.MarkAsRead(id));
                PopupNotifications.IsOpen = false;
                
                _ = Task.Run(() =>
                {
                    using var db = new ArcelikApp.Data.AppDbContext();
                    var notification = db.Notifications.Find(id);
                    if (notification != null)
                    {
                        var type = notification.Type switch
                        {
                            "Success" => ModernDialogType.Success,
                            "Warning" => ModernDialogType.Warning,
                            "Error" => ModernDialogType.Error,
                            _ => ModernDialogType.Info
                        };

                        Dispatcher.Invoke(() => _ = ModernDialogService.ShowAsync(notification.Title, notification.Message, type));
                    }
                });
            }
        }

        private void BtnViewAllNotifications_Click(object sender, RoutedEventArgs e)
        {
            PopupNotifications.IsOpen = false;
            TxtPageTitle.Text = "Tüm Bildirimler";
            MainContentControl.Content = new NotificationsView();
        }
        #endregion

        #region Navigation
        private void ResetNavButtons(Panel parent)
        {
            foreach (var child in parent.Children)
            {
                if (child is Button menuBtn)
                {
                    menuBtn.ClearValue(Button.BackgroundProperty);
                    menuBtn.ClearValue(Button.ForegroundProperty);
                }
                else if (child is Panel panel)
                {
                    ResetNavButtons(panel);
                }
            }
        }

        private async void Nav_Click(object sender, RoutedEventArgs e)
        {
            // Session kontrolünü cache'le — her tıklamada DB'ye gitmesin
            if (DateTime.Now - _lastSessionCheck > SessionCheckInterval)
            {
                try
                {
                    _lastSessionValid = await Task.Run(() => AuthService.IsSessionValid());
                    _lastSessionCheck = DateTime.Now;
                }
                catch
                {
                    // Ağ hatası — son durumu koru
                }
            }

            if (!_lastSessionValid)
            {
                _ = ModernDialogService.ShowAsync("Oturum Hatası", "Oturumunuz sonlandırıldı. Başka bir cihazdan giriş yapılmış olabilir.", ModernDialogType.Error);
                BtnLogout_Click(null, null);
                return;
            }

            if (sender is Button btn && btn.Tag is string tag)
            {
                if (MenuStackPanel != null)
                {
                    ResetNavButtons(MenuStackPanel);
                }

                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E02020"));
                btn.Foreground = Brushes.White;

                switch (tag)
                {
                    case "Anasayfa":
                        TxtPageTitle.Text = "Dashboard";
                        MainContentControl.Content = new DashboardView();
                        break;
                    case "Kea":
                        TxtPageTitle.Text = "Küçük Ev Aletleri";
                        MainContentControl.Content = new KeaView();
                        break;
                    case "BeyazEsya":
                        TxtPageTitle.Text = "Beyaz Eşya";
                        MainContentControl.Content = new BeyazEsyaView();
                        break;
                    case "YeniFiyat":
                        TxtPageTitle.Text = "Maliyet Hesaplama";
                        MainContentControl.Content = new YeniFiyatView();
                        break;
                    case "ExcelViewer":
                        TxtPageTitle.Text = "Excel Görüntüleyici";
                        MainContentControl.Content = new ExcelViewer();
                        break;
                    case "UserManagement":
                        if (AuthService.CurrentUser?.Role != "Admin")
                        {
                            _ = ModernDialogService.ShowAsync("Yetki Hatası", "Bu bölüme sadece yöneticiler erişebilir.", ModernDialogType.Warning);
                            return;
                        }
                        TxtPageTitle.Text = "Kullanıcı Yönetimi";
                        MainContentControl.Content = new UserManagementView();
                        break;
                    case "Excell":
                        if (AuthService.CurrentUser?.Role != "Admin")
                        {
                            _ = ModernDialogService.ShowAsync("Yetki Hatası", "Bu bölüme sadece yöneticiler erişebilir.", ModernDialogType.Warning);
                            return;
                        }
                        TxtPageTitle.Text = "Excel İçeri Aktar";
                        MainContentControl.Content = new ExcelIslemleriView();
                        break;
                    case "FileManagement":
                        if (AuthService.CurrentUser?.Role != "Admin")
                        {
                            _ = ModernDialogService.ShowAsync("Yetki Hatası", "Bu bölüme sadece yöneticiler erişebilir.", ModernDialogType.Warning);
                            return;
                        }
                        TxtPageTitle.Text = "Dosya Yönetimi";
                        MainContentControl.Content = new FileManagementView();
                        break;
                    case "Settings":
                        TxtPageTitle.Text = "Ayarlar";
                        MainContentControl.Content = new SettingsView();
                        break;
                }
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() => AuthService.Logout());
            LoginWindow login = new LoginWindow();
            login.Visibility = Visibility.Visible;
            login.Show();
            this.Close();
        }
        #endregion
    }
}
