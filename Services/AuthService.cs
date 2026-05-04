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
                user.LicenseExpirationDate = DateTime.Now.AddYears(1);
            }
            else
            {
                // Cihaz Kontrolü (Başka cihazda girilemez)
                if (user.DeviceId != currentDeviceId)
                {
                    return new LoginResult { Success = false, Message = "Bu hesap başka bir cihazda aktifleştirilmiş. Bu cihazda kullanılamaz." };
                }

                // Lisans Süresi Kontrolü
                if (user.LicenseExpirationDate.HasValue && user.LicenseExpirationDate.Value < DateTime.Now)
                {
                    if (string.IsNullOrEmpty(licenseKey))
                        return new LoginResult { Success = false, Message = "Lisans süreniz dolmuştur. Lütfen yeni bir lisans anahtarı giriniz.", NeedsActivation = true };
                    
                    if (user.LicenseKey != licenseKey)
                        return new LoginResult { Success = false, Message = "Geçersiz lisans anahtarı." };
                    
                    // Yeni lisans aktif
                    user.LicenseExpirationDate = DateTime.Now.AddYears(1);
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

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return new LoginResult { Success = false, Message = $"Giriş sırasında veritabanı hatası oluştu: {ex.Message}" };
            }

            CurrentUser = user;
            SessionId = newSessionId;

            // Sözleşme kontrolü
            if (MustAcceptLatestAgreement(user.Id))
            {
                var latest = GetLatestAgreement();
                return new LoginResult 
                { 
                    Success = false, 
                    Message = "Yeni bir kullanıcı sözleşmesi yayınlandı. Devam etmek için onaylamanız gerekmektedir.",
                    NeedsAgreementAcceptance = true,
                    LatestAgreementId = latest?.Id,
                    User = user 
                };
            }

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
            
            // Create initial agreement if none exists
            if (!db.Agreements.Any())
            {
                var agreement = new Agreement
                {
                    Version = "v1.0",
                    Content = "Synctool Kullanıcı Sözleşmesi\n\n" +
                              "1. Taraflar\n" +
                              "Bu sözleşme, Synctool uygulamasını kullanan kişi/kurum (bundan böyle 'Kullanıcı' olarak anılacaktır) ile uygulama sahibi arasında akdedilmiştir.\n\n" +
                              "2. Kullanım Koşulları\n" +
                              "Kullanıcı, uygulamayı yalnızca yasal amaçlar için ve sözleşme şartlarına uygun olarak kullanmayı kabul eder.\n\n" +
                              "3. Gizlilik ve Veri Güvenliği\n" +
                              "Kullanıcıya ait bilgiler üçüncü şahıslarla paylaşılmayacaktır. Tüm veriler şifrelenerek saklanmaktadır.\n\n" +
                              "4. Lisans Süresi ve İptali\n" +
                              "Lisans anahtarları aksi belirtilmedikçe 1 yıl geçerlidir. Süre bitiminde yeni lisans alınması gerekmektedir.\n\n" +
                              "Tarih: " + DateTime.Now.ToString("dd.MM.yyyy")
                };
                db.Agreements.Add(agreement);
                db.SaveChanges();
            }

            if (!db.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    PasswordHash = SecurityHelper.HashPassword("admin123"),
                    Role = "Admin",
                    LicenseKey = "ADMIN-KEY-001",
                    IsActive = true,
                    IsActivated = true,
                    LicenseExpirationDate = DateTime.Now.AddYears(10)
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

        public static RegisterResult Register(string username, string dealerName, string password, int agreementId)
        {
            using var db = new AppDbContext();
            if (db.Users.Any(u => u.Username == username))
            {
                return new RegisterResult { Success = false, Message = "Bu kullanıcı adı zaten alınmış." };
            }

            var newUser = new User
            {
                Username = username,
                DealerName = dealerName,
                PasswordHash = SecurityHelper.HashPassword(password),
                Role = "User",
                IsActive = true,
                IsActivated = false
            };

            db.Users.Add(newUser);
            db.SaveChanges(); // to get the new User ID

            AcceptAgreement(newUser.Id, agreementId);

            return new RegisterResult { Success = true };
        }

        public static bool MustAcceptLatestAgreement(int userId)
        {
            using var db = new AppDbContext();
            var latestAgreement = db.Agreements.OrderByDescending(a => a.Id).FirstOrDefault();
            if (latestAgreement == null) return false;

            var userConsent = db.UserConsents.FirstOrDefault(c => c.UserId == userId && c.AgreementId == latestAgreement.Id);
            return userConsent == null;
        }

        public static Agreement? GetLatestAgreement()
        {
            using var db = new AppDbContext();
            return db.Agreements.OrderByDescending(a => a.Id).FirstOrDefault();
        }

        public static bool AcceptAgreement(int userId, int agreementId)
        {
            using var db = new AppDbContext();
            // Check if already accepted
            if (db.UserConsents.Any(c => c.UserId == userId && c.AgreementId == agreementId))
                return true;

            var consent = new UserConsent
            {
                UserId = userId,
                AgreementId = agreementId,
                AcceptedAt = DateTime.Now,
                IpAddress = SecurityHelper.GetLocalIPAddress()
            };
            db.UserConsents.Add(consent);
            db.SaveChanges();
            return true;
        }

        public static bool SaveNewAgreement(string version, string content)
        {
            using var db = new AppDbContext();
            var agreement = new Agreement
            {
                Version = version,
                Content = content,
                CreatedAt = DateTime.Now
            };
            db.Agreements.Add(agreement);
            db.SaveChanges();
            return true;
        }
    }

    public class RegisterResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool NeedsActivation { get; set; }
        public bool NeedsAgreementAcceptance { get; set; }
        public int? LatestAgreementId { get; set; }
        public User? User { get; set; }
    }
}
