using ArcelikApp.Data;
using ArcelikApp.Models;
using ArcelikApp.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ArcelikExcelApp.Views
{
    public partial class UserManagementView : UserControl
    {
        private User? _editingUser = null;

        public UserManagementView()
        {
            InitializeComponent();
            TxtSearch.TextChanged += TxtSearch_TextChanged;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = TxtSearch.Text.ToLower();
            if (GridUsers.ItemsSource is System.Collections.Generic.List<User> users)
            {
                var view = CollectionViewSource.GetDefaultView(GridUsers.ItemsSource);
                view.Filter = (obj) =>
                {
                    if (obj is User user)
                    {
                        return user.Username.ToLower().Contains(filter) || user.Role.ToLower().Contains(filter);
                    }
                    return false;
                };
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                using var db = new AppDbContext();
                var users = db.Users.ToList();
                GridUsers.ItemsSource = users;

                // Populate notification user dropdown
                if (CmbNotifyUser != null)
                {
                    CmbNotifyUser.Items.Clear();
                    CmbNotifyUser.Items.Add(new ComboBoxItem { Content = "Tüm Kullanıcılar (Herkes)", Tag = "All", IsSelected = true });
                    CmbNotifyUser.Items.Add(new ComboBoxItem { Content = "Tüm Adminler", Tag = "Role:Admin" });
                    CmbNotifyUser.Items.Add(new ComboBoxItem { Content = "Tüm Normal Kullanıcılar", Tag = "Role:User" });
                    
                    foreach (var u in users)
                    {
                        CmbNotifyUser.Items.Add(new ComboBoxItem { Content = $"{u.Username} ({u.Role})", Tag = u.Id });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcılar yüklenirken hata: {ex.Message}");
            }
        }

        private void BtnOpenNotificationDialog_Click(object sender, RoutedEventArgs e)
        {
            TxtNotifyTitle.Text = "";
            TxtNotifyMessage.Text = "";
            TxtNotifyError.Visibility = Visibility.Collapsed;
            NotificationDialogOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelNotify_Click(object sender, RoutedEventArgs e)
        {
            NotificationDialogOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSendNotification_Click(object sender, RoutedEventArgs e)
        {
            string title = TxtNotifyTitle.Text.Trim();
            string message = TxtNotifyMessage.Text.Trim();
            var selectedItem = CmbNotifyUser.SelectedItem as ComboBoxItem;
            var selectedType = CmbNotifyType.SelectedItem as ComboBoxItem;

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(message))
            {
                TxtNotifyError.Text = "Lütfen başlık ve mesaj alanlarını doldurun.";
                TxtNotifyError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                string type = selectedType?.Tag?.ToString() ?? "Info";
                ModernDialogType dialogType = type == "Success" ? ModernDialogType.Success : 
                                            type == "Warning" ? ModernDialogType.Warning : 
                                            type == "Error" ? ModernDialogType.Error : ModernDialogType.Info;

                string tag = selectedItem?.Tag?.ToString() ?? "";
                
                if (tag == "All")
                {
                    NotificationService.SendToAll(title, message, type);
                }
                else if (tag.StartsWith("Role:"))
                {
                    string role = tag.Substring(5);
                    NotificationService.SendToRole(role, title, message, type);
                }
                else if (int.TryParse(tag, out int userId))
                {
                    NotificationService.SendNotification(userId, title, message, type);
                }

                NotificationDialogOverlay.Visibility = Visibility.Collapsed;
                _ = ModernDialogService.ShowAsync("Başarılı", "Bildirim başarıyla gönderildi.", ModernDialogType.Success);
            }
            catch (Exception ex)
            {
                TxtNotifyError.Text = $"Hata: {ex.Message}";
                TxtNotifyError.Visibility = Visibility.Visible;
            }
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            _editingUser = null;
            TxtDialogTitle.Text = "Yeni Kullanıcı";
            TxtNewUsername.Text = "";
            TxtNewPassword.Password = "";
            TxtNewLicenseKey.Text = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            TxtUserError.Visibility = Visibility.Collapsed;
            DialogOverlay.Visibility = Visibility.Visible;
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                _editingUser = user;
                TxtDialogTitle.Text = "Kullanıcıyı Düzenle";
                TxtNewUsername.Text = user.Username;
                TxtNewPassword.Password = ""; // Şifre boş kalsın (değişmeyecekse)
                TxtNewLicenseKey.Text = user.LicenseKey;
                CmbRole.SelectedIndex = user.Role == "Admin" ? 1 : 0;
                TxtUserError.Visibility = Visibility.Collapsed;
                DialogOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnSaveUser_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtNewUsername.Text.Trim();
            string password = TxtNewPassword.Password;
            string role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User";
            string license = TxtNewLicenseKey.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                ShowUserError("Kullanıcı adı boş olamaz.");
                return;
            }

            if (_editingUser == null && string.IsNullOrEmpty(password))
            {
                ShowUserError("Yeni kullanıcı için şifre gereklidir.");
                return;
            }

            try
            {
                using var db = new AppDbContext();
                if (_editingUser == null)
                {
                    // Yeni kullanıcı
                    if (db.Users.Any(u => u.Username == username))
                    {
                        ShowUserError("Bu kullanıcı adı zaten alınmış.");
                        return;
                    }

                    var newUser = new User
                    {
                        Username = username,
                        PasswordHash = SecurityHelper.HashPassword(password),
                        Role = role,
                        LicenseKey = license,
                        IsActive = true
                    };
                    db.Users.Add(newUser);
                }
                else
                {
                    // Düzenle
                    var user = db.Users.Find(_editingUser.Id);
                    if (user != null)
                    {
                        // Check if username changed and is already taken
                        if (user.Username != username && db.Users.Any(u => u.Username == username))
                        {
                            ShowUserError("Bu kullanıcı adı zaten alınmış.");
                            return;
                        }

                        user.Username = username;
                        if (!string.IsNullOrEmpty(password))
                            user.PasswordHash = SecurityHelper.HashPassword(password);
                        user.Role = role;
                        user.LicenseKey = license;
                    }
                }

                db.SaveChanges();
                DialogOverlay.Visibility = Visibility.Collapsed;
                LoadUsers();
            }
            catch (Exception ex)
            {
                ShowUserError($"Sistem Hatası: {ex.Message}");
            }
        }

        private void ShowUserError(string message)
        {
            TxtUserError.Text = message;
            TxtUserError.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnResetSession_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                try
                {
                    using var db = new AppDbContext();
                    var dbUser = db.Users.Find(user.Id);
                    if (dbUser != null)
                    {
                        dbUser.CurrentSessionId = null;
                        db.SaveChanges();
                        _ = ModernDialogService.ShowAsync("Başarılı", $"{user.Username} kullanıcısının oturumu sıfırlandı.", ModernDialogType.Success);
                        LoadUsers();
                    }
                }
                catch (Exception ex)
                {
                    _ = ModernDialogService.ShowAsync("Hata", $"Hata: {ex.Message}", ModernDialogType.Error);
                }
            }
        }

        private async void BtnResetDevice_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                var result = await ModernDialogService.ShowAsync("Cihaz Sıfırla", $"{user.Username} kullanıcısının cihaz kilidini sıfırlamak istiyor musunuz? Kullanıcı yeni cihazında tekrar aktivasyon yapabilecektir.", ModernDialogType.Question);
                
                if (result)
                {
                    try
                    {
                        using var db = new AppDbContext();
                        var dbUser = db.Users.Find(user.Id);
                        if (dbUser != null)
                        {
                            dbUser.DeviceId = null;
                            dbUser.IsActivated = false;
                            db.SaveChanges();
                            _ = ModernDialogService.ShowAsync("Başarılı", "Cihaz kilidi başarıyla kaldırıldı.", ModernDialogType.Success);
                            LoadUsers();
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = ModernDialogService.ShowAsync("Hata", $"Hata: {ex.Message}", ModernDialogType.Error);
                    }
                }
            }
        }
    }
}
