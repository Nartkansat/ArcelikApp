using ArcelikApp.Data;
using ArcelikApp.Models;
using ArcelikApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

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
            if (GridUsers.ItemsSource is List<User>)
            {
                var view = CollectionViewSource.GetDefaultView(GridUsers.ItemsSource);
                view.Filter = (obj) =>
                {
                    if (obj is User user)
                        return user.Username.ToLower().Contains(filter) || user.Role.ToLower().Contains(filter);
                    return false;
                };
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    return db.Users.ToList();
                });

                GridUsers.ItemsSource = users;

                if (CmbNotifyUser != null)
                {
                    CmbNotifyUser.Items.Clear();
                    CmbNotifyUser.Items.Add(new ComboBoxItem { Content = "Tüm Kullanıcılar (Herkes)", Tag = "All", IsSelected = true });
                    CmbNotifyUser.Items.Add(new ComboBoxItem { Content = "Tüm Adminler", Tag = "Role:Admin" });
                    CmbNotifyUser.Items.Add(new ComboBoxItem { Content = "Tüm Normal Kullanıcılar", Tag = "Role:User" });
                    foreach (var u in users)
                        CmbNotifyUser.Items.Add(new ComboBoxItem { Content = $"{u.Username} ({u.Role})", Tag = u.Id });
                }
            }
            catch (Exception ex)
            {
                await ModernDialogService.ShowAsync("Hata", $"Kullanıcılar yüklenirken hata oluştu: {ex.Message}", ModernDialogType.Error);
            }
        }

        // ─── Yeni Kullanıcı Ekle ────────────────────────────────────────────
        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            TxtNewUsername.Text = "";
            TxtNewEmail.Text = "";
            TxtNewDealerName.Text = "";
            TxtNewPassword.Password = "";
            TxtNewPasswordVisible.Text = "";
            TxtNewPasswordConfirm.Password = "";
            TxtNewPasswordConfirmVisible.Text = "";
            // Reset password show/hide state
            TxtNewPassword.Visibility = Visibility.Visible;
            TxtNewPasswordVisible.Visibility = Visibility.Collapsed;
            IconAddShowPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
            IconAddShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            TxtNewPasswordConfirm.Visibility = Visibility.Visible;
            TxtNewPasswordConfirmVisible.Visibility = Visibility.Collapsed;
            IconAddShowPasswordConfirm.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
            IconAddShowPasswordConfirm.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

            CmbRole.SelectedIndex = 0;
            using (var db = new AppDbContext())
                TxtNewLicenseKey.Text = AuthService.GenerateUniqueLicenseKey(db);

            TxtUserError.Visibility = Visibility.Collapsed;
            DialogOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnSaveUser_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtNewUsername.Text.Trim();
            string email = TxtNewEmail.Text.Trim();
            string dealerName = TxtNewDealerName.Text.Trim();
            string password = TxtNewPasswordVisible.IsVisible ? TxtNewPasswordVisible.Text : TxtNewPassword.Password;
            string passwordConfirm = TxtNewPasswordConfirmVisible.IsVisible ? TxtNewPasswordConfirmVisible.Text : TxtNewPasswordConfirm.Password;
            string role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User";
            string license = TxtNewLicenseKey.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(dealerName))
            {
                ShowUserError("Lütfen tüm alanları doldurun.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowUserError("Şifre boş olamaz.");
                return;
            }

            if (password != passwordConfirm)
            {
                ShowUserError("Şifreler uyuşmuyor.");
                return;
            }

            if (password.Length < 6)
            {
                ShowUserError("Şifre en az 6 karakter olmalıdır.");
                return;
            }

            try
            {
                string? errorMsg = await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    if (db.Users.Any(u => u.Username == username))
                        return "Bu kullanıcı adı zaten alınmış.";

                    var newUser = new User
                    {
                        Username = username,
                        Email = email,
                        DealerName = dealerName,
                        PasswordHash = SecurityHelper.HashPassword(password),
                        Role = role,
                        LicenseKey = string.IsNullOrEmpty(license) ? AuthService.GenerateUniqueLicenseKey(db) : license,
                        IsActive = true,
                        IsActivated = false
                    };
                    db.Users.Add(newUser);
                    db.SaveChanges();
                    return null;
                });

                if (errorMsg != null) { ShowUserError(errorMsg); return; }

                DialogOverlay.Visibility = Visibility.Collapsed;
                _ = ModernDialogService.ShowAsync("Başarılı", "Yeni kullanıcı başarıyla oluşturuldu.", ModernDialogType.Success);
                await LoadUsersAsync();
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

        // ─── Şifre Göster/Gizle (Yeni Kullanıcı) ───────────────────────────
        private void BtnAddShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtNewPassword.Visibility == Visibility.Visible)
            {
                TxtNewPasswordVisible.Text = TxtNewPassword.Password;
                TxtNewPassword.Visibility = Visibility.Collapsed;
                TxtNewPasswordVisible.Visibility = Visibility.Visible;
                IconAddShowPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
                IconAddShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E02020"));
                TxtNewPasswordVisible.Focus();
            }
            else
            {
                TxtNewPassword.Password = TxtNewPasswordVisible.Text;
                TxtNewPasswordVisible.Visibility = Visibility.Collapsed;
                TxtNewPassword.Visibility = Visibility.Visible;
                IconAddShowPassword.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
                IconAddShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtNewPassword.Focus();
            }
        }

        private void BtnAddShowPasswordConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (TxtNewPasswordConfirm.Visibility == Visibility.Visible)
            {
                TxtNewPasswordConfirmVisible.Text = TxtNewPasswordConfirm.Password;
                TxtNewPasswordConfirm.Visibility = Visibility.Collapsed;
                TxtNewPasswordConfirmVisible.Visibility = Visibility.Visible;
                IconAddShowPasswordConfirm.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
                IconAddShowPasswordConfirm.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E02020"));
                TxtNewPasswordConfirmVisible.Focus();
            }
            else
            {
                TxtNewPasswordConfirm.Password = TxtNewPasswordConfirmVisible.Text;
                TxtNewPasswordConfirmVisible.Visibility = Visibility.Collapsed;
                TxtNewPasswordConfirm.Visibility = Visibility.Visible;
                IconAddShowPasswordConfirm.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
                IconAddShowPasswordConfirm.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtNewPasswordConfirm.Focus();
            }
        }

        // ─── Kullanıcı Düzenle (Sadece Rol) ─────────────────────────────────
        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                _editingUser = user;
                TxtEditUserSubtitle.Text = $"{user.Username} kullanıcısının yetkisini düzenleyin";
                CmbEditRole.SelectedIndex = user.Role == "Admin" ? 1 : 0;
                TxtEditUserError.Visibility = Visibility.Collapsed;
                EditUserDialogOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditUserDialogOverlay.Visibility = Visibility.Collapsed;
            _editingUser = null;
        }

        private async void BtnSaveEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (_editingUser == null) return;

            string role = (CmbEditRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User";
            int userId = _editingUser.Id;

            try
            {
                await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    var dbUser = db.Users.Find(userId);
                    if (dbUser != null)
                    {
                        dbUser.Role = role;
                        db.SaveChanges();
                    }
                });

                EditUserDialogOverlay.Visibility = Visibility.Collapsed;
                _editingUser = null;
                _ = ModernDialogService.ShowAsync("Başarılı", "Kullanıcı yetkisi güncellendi.", ModernDialogType.Success);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                TxtEditUserError.Text = $"Hata: {ex.Message}";
                TxtEditUserError.Visibility = Visibility.Visible;
            }
        }

        // ─── Hesabı Kilitle ──────────────────────────────────────────────────
        private async void BtnLockUser_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                var result = await ModernDialogService.ShowAsync("Hesabı Askıya Al",
                    $"{user.Username} kullanıcısının hesabını askıya almak istiyor musunuz? Kullanıcı giriş yapamayacaktır.",
                    ModernDialogType.Question);

                if (result)
                {
                    try
                    {
                        int userId = user.Id;
                        string userName = user.Username;
                        await Task.Run(() =>
                        {
                            using var db = new AppDbContext();
                            var dbUser = db.Users.Find(userId);
                            if (dbUser != null)
                            {
                                dbUser.IsLocked = true;
                                db.SaveChanges();
                            }
                        });
                        _ = ModernDialogService.ShowAsync("Başarılı", $"{userName} kullanıcısının hesabı askıya alındı.", ModernDialogType.Success);
                        await LoadUsersAsync();
                    }
                    catch (Exception ex)
                    {
                        _ = ModernDialogService.ShowAsync("Hata", $"Hata: {ex.Message}", ModernDialogType.Error);
                    }
                }
            }
        }

        // ─── Hesap Kilidini Kaldır ───────────────────────────────────────────
        private async void BtnUnlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                try
                {
                    int userId = user.Id;
                    string userName = user.Username;
                    await Task.Run(() =>
                    {
                        using var db = new AppDbContext();
                        var dbUser = db.Users.Find(userId);
                        if (dbUser != null)
                        {
                            dbUser.IsLocked = false;
                            dbUser.FailedLoginAttempts = 0;
                            db.SaveChanges();
                        }
                    });
                    _ = ModernDialogService.ShowAsync("Başarılı", $"{userName} kullanıcısının hesabı tekrar aktif edildi.", ModernDialogType.Success);
                    await LoadUsersAsync();
                }
                catch (Exception ex)
                {
                    _ = ModernDialogService.ShowAsync("Hata", $"Hata: {ex.Message}", ModernDialogType.Error);
                }
            }
        }

        // ─── Oturum Sıfırla ──────────────────────────────────────────────────
        private async void BtnResetSession_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                try
                {
                    int userId = user.Id;
                    string userName = user.Username;
                    await Task.Run(() =>
                    {
                        using var db = new AppDbContext();
                        var dbUser = db.Users.Find(userId);
                        if (dbUser != null)
                        {
                            dbUser.CurrentSessionId = null;
                            db.SaveChanges();
                        }
                    });
                    _ = ModernDialogService.ShowAsync("Başarılı", $"{userName} kullanıcısının oturumu sıfırlandı.", ModernDialogType.Success);
                    await LoadUsersAsync();
                }
                catch (Exception ex)
                {
                    _ = ModernDialogService.ShowAsync("Hata", $"Hata: {ex.Message}", ModernDialogType.Error);
                }
            }
        }

        // ─── Cihaz Sıfırla ───────────────────────────────────────────────────
        private async void BtnResetDevice_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is User user)
            {
                var result = await ModernDialogService.ShowAsync("Cihaz Sıfırla",
                    $"{user.Username} kullanıcısının cihaz kilidini sıfırlamak istiyor musunuz? Kullanıcı yeni cihazında tekrar aktivasyon yapabilecektir.",
                    ModernDialogType.Question);

                if (result)
                {
                    try
                    {
                        int userId = user.Id;
                        await Task.Run(() =>
                        {
                            using var db = new AppDbContext();
                            var dbUser = db.Users.Find(userId);
                            if (dbUser != null)
                            {
                                dbUser.DeviceId = null;
                                dbUser.HardwareCpuId = null;
                                dbUser.HardwareMotherboardId = null;
                                dbUser.HardwareDiskId = null;
                                dbUser.IsActivated = false;
                                db.SaveChanges();
                            }
                        });
                        _ = ModernDialogService.ShowAsync("Başarılı", $"{user.Username} kullanıcısının cihaz kaydı sıfırlandı. Kullanıcı bir sonraki girişinde yeni cihazını aktive edebilecek.", ModernDialogType.Success);
                        await LoadUsersAsync();
                    }
                    catch (Exception ex)
                    {
                        _ = ModernDialogService.ShowAsync("Hata", $"Hata: {ex.Message}", ModernDialogType.Error);
                    }
                }
            }
        }

        // ─── Bildirim Gönder ─────────────────────────────────────────────────
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

        private async void BtnSendNotification_Click(object sender, RoutedEventArgs e)
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
                string tag = selectedItem?.Tag?.ToString() ?? "";

                await Task.Run(() =>
                {
                    if (tag == "All")
                        NotificationService.SendToAll(title, message, type);
                    else if (tag.StartsWith("Role:"))
                        NotificationService.SendToRole(tag.Substring(5), title, message, type);
                    else if (int.TryParse(tag, out int userId))
                        NotificationService.SendNotification(userId, title, message, type);
                });

                NotificationDialogOverlay.Visibility = Visibility.Collapsed;
                _ = ModernDialogService.ShowAsync("Başarılı", "Bildirim başarıyla gönderildi.", ModernDialogType.Success);
            }
            catch (Exception ex)
            {
                TxtNotifyError.Text = $"Hata: {ex.Message}";
                TxtNotifyError.Visibility = Visibility.Visible;
            }
        }
    }
}
