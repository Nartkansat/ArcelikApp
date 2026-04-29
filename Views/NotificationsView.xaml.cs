using ArcelikApp.Services;
using System.Windows;
using System.Windows.Controls;

namespace ArcelikExcelApp.Views
{
    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            if (AuthService.CurrentUser == null) return;
            
            var list = NotificationService.GetUserNotifications(AuthService.CurrentUser.Id);
            GridNotifications.ItemsSource = list;
            EmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnMarkRead_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                NotificationService.MarkAsRead(id);
                LoadNotifications();
            }
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
                    NotificationService.MarkAllAsRead(AuthService.CurrentUser.Id);
                    LoadNotifications();
                    _ = ModernDialogService.ShowAsync("Başarılı", "Tüm bildirimler okundu olarak işaretlendi.", ModernDialogType.Success);
                }
            }
        }

        private void BtnShowDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                NotificationService.MarkAsRead(id);
                
                using var db = new ArcelikApp.Data.AppDbContext();
                var notification = db.Notifications.Find(id);
                if (notification != null)
                {
                    TxtDetailTitle.Text = notification.Title;
                    TxtDetailMessage.Text = notification.Message;
                    TxtDetailDate.Text = notification.CreatedAt.ToString("dd MMMM yyyy HH:mm");

                    var type = notification.Type ?? "Info";
                    switch (type)
                    {
                        case "Success":
                            BorderDetailIcon.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8F5E9"));
                            IconDetail.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
                            IconDetail.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircle;
                            break;
                        case "Warning":
                            BorderDetailIcon.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF3E0"));
                            IconDetail.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF9800"));
                            IconDetail.Kind = MaterialDesignThemes.Wpf.PackIconKind.Alert;
                            break;
                        case "Error":
                            BorderDetailIcon.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFEBEE"));
                            IconDetail.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336"));
                            IconDetail.Kind = MaterialDesignThemes.Wpf.PackIconKind.AlertOctagon;
                            break;
                        default:
                            BorderDetailIcon.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E3F2FD"));
                            IconDetail.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2196F3"));
                            IconDetail.Kind = MaterialDesignThemes.Wpf.PackIconKind.Information;
                            break;
                    }

                    NotificationDetailOverlay.Visibility = Visibility.Visible;
                }
                LoadNotifications();
            }
        }

        private void BtnCloseDetail_Click(object sender, RoutedEventArgs e)
        {
            NotificationDetailOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var result = await ModernDialogService.ShowAsync("Bildirim Sil", "Bu bildirimi silmek istediğinize emin misiniz?", ModernDialogType.Question);
                if (result)
                {
                    NotificationService.DeleteNotification(id);
                    LoadNotifications();
                }
            }
        }

        private async void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser != null)
            {
                var result = await ModernDialogService.ShowAsync("Tümünü Sil", "Tüm bildirimlerinizi silmek istediğinize emin misiniz? Bu işlem geri alınamaz.", ModernDialogType.Question);
                if (result)
                {
                    NotificationService.DeleteAllNotifications(AuthService.CurrentUser.Id);
                    LoadNotifications();
                }
            }
        }
    }
}
