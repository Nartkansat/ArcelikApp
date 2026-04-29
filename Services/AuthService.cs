using ArcelikApp.Data;
using ArcelikApp.Models;
using System;
using System.Linq;

namespace ArcelikApp.Services
{
    public class AuthService
    {
        public static User? CurrentUser { get; private set; }
        public static string? SessionId { get; private set; }

        public static LoginResult Login(string username, string password, string? licenseKey, bool rememberMe)
        {
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null || !SecurityHelper.VerifyPassword(password, user.PasswordHash))
                return new LoginResult { Success = false, Message = "Kullanıcı adı veya şifre hatalı." };

            if (!user.IsActive)
                return new LoginResult { Success = false, Message = "Hesabınız pasif durumdadır." };

            string currentDeviceId = SecurityHelper.GetDeviceId();

            // İlk Giriş / Aktivasyon Kontrolü
            if (!user.IsActivated)
            {
                if (string.IsNullOrEmpty(licenseKey))
                    return new LoginResult { Success = false, Message = "Hesap henüz aktif değil. Lütfen lisans anahtarını giriniz.", NeedsActivation = true };

                if (user.LicenseKey != licenseKey)
                    return new LoginResult { Success = false, Message = "Geçersiz lisans anahtarı." };

                // Aktivasyon başarılı
                user.IsActivated = true;
                user.DeviceId = currentDeviceId;
            }
            else
            {
                // Cihaz Kontrolü (Başka cihazda girilemez)
                if (user.DeviceId != currentDeviceId)
                {
                    return new LoginResult { Success = false, Message = "Bu hesap başka bir cihazda aktifleştirilmiş. Bu cihazda kullanılamaz." };
                }
            }

            // Oturum ID'si oluştur (Tek kişi girme kuralı için)
            string newSessionId = SecurityHelper.GenerateToken();
            user.CurrentSessionId = newSessionId;
            user.LastLoginDate = DateTime.Now;

            if (rememberMe)
            {
                if (string.IsNullOrEmpty(user.RememberMeToken))
                    user.RememberMeToken = SecurityHelper.GenerateToken();
                
                user.TokenExpiry = DateTime.Now.AddDays(30);
                TokenStorage.SaveToken(user.RememberMeToken);
            }
            else
            {
                user.RememberMeToken = null;
                user.TokenExpiry = null;
                TokenStorage.ClearToken();
            }

            db.SaveChanges();

            CurrentUser = user;
            SessionId = newSessionId;

            return new LoginResult { Success = true, User = user };
        }

        public static bool CheckAutoLogin()
        {
            string? token = TokenStorage.GetToken();
            if (string.IsNullOrEmpty(token)) return false;

            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.RememberMeToken == token && u.TokenExpiry > DateTime.Now);

            if (user != null && user.IsActive)
            {
                // Cihaz Kontrolü
                if (user.DeviceId != SecurityHelper.GetDeviceId())
                {
                    TokenStorage.ClearToken();
                    return false;
                }

                string newSessionId = SecurityHelper.GenerateToken();
                user.CurrentSessionId = newSessionId;
                user.LastLoginDate = DateTime.Now;
                db.SaveChanges();

                CurrentUser = user;
                SessionId = newSessionId;
                return true;
            }

            TokenStorage.ClearToken();
            return false;
        }

        public static void Logout()
        {
            TokenStorage.ClearToken();
            if (CurrentUser != null)
            {
                using var db = new AppDbContext();
                var user = db.Users.Find(CurrentUser.Id);
                if (user != null)
                {
                    user.CurrentSessionId = null;
                    db.SaveChanges();
                }
            }
            CurrentUser = null;
            SessionId = null;
        }

        /// <summary>
        /// Oturumun hala geçerli olup olmadığını kontrol eder (Başka biri girdi mi?)
        /// </summary>
        public static bool IsSessionValid()
        {
            if (CurrentUser == null || SessionId == null) return false;

            using var db = new AppDbContext();
            var user = db.Users.Find(CurrentUser.Id);
            return user != null && user.CurrentSessionId == SessionId;
        }

        public static void CreateInitialAdmin()
        {
            using var db = new AppDbContext();
            if (!db.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    PasswordHash = SecurityHelper.HashPassword("admin123"),
                    Role = "Admin",
                    LicenseKey = "ADMIN-KEY-001",
                    IsActive = true
                };
                db.Users.Add(admin);
                db.SaveChanges();
            }
        }

        public static bool ResetPassword(string username, string licenseKey, string newPassword)
        {
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.Username == username && u.LicenseKey == licenseKey);
            
            if (user == null) return false;

            user.PasswordHash = SecurityHelper.HashPassword(newPassword);
            db.SaveChanges();
            return true;
        }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool NeedsActivation { get; set; }
        public User? User { get; set; }
    }
}
