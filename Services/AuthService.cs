using ArcelikApp.Data;
using ArcelikApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace ArcelikApp.Services
{
    public class AuthService
    {
        public static User? CurrentUser { get; private set; }
        public static string? SessionId { get; private set; }

        public static async System.Threading.Tasks.Task<LoginResult> LoginAsync(string username, string password, string? licenseKey, bool rememberMe)
        {
            using var db = new AppDbContext();
            
            // Asenkron olarak veritabanı sorgusu ve donanım kimliği okuma işlemlerini paralel başlat
            var userTask = db.Users.FirstOrDefaultAsync(u => u.Username == username);
            var hwProfileTask = SecurityHelper.GetHardwareProfileAsync();
            
            await System.Threading.Tasks.Task.WhenAll(userTask, hwProfileTask);
            var user = userTask.Result;
            var currentProfile = hwProfileTask.Result;

            if (user == null)
                return new LoginResult { Success = false, Message = "Kullanıcı adı veya şifre hatalı." };

            if (user.IsLocked)
                return new LoginResult { Success = false, Message = "Hesabınız çok sayıda hatalı giriş denemesi nedeniyle askıya alınmıştır. Lütfen yönetici ile iletişime geçin." };

            if (!user.IsActive)
                return new LoginResult { Success = false, Message = "Hesabınız pasif durumdadır." };

            if (!SecurityHelper.VerifyPassword(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsLocked = true;
                    await db.SaveChangesAsync();
                    return new LoginResult { Success = false, Message = "Hesabınız 5 kez hatalı giriş denemesi nedeniyle askıya alınmıştır." };
                }
                
                await db.SaveChangesAsync();
                return new LoginResult { Success = false, Message = $"Kullanıcı adı veya şifre hatalı. (Kalan deneme hakkı: {5 - user.FailedLoginAttempts})" };
            }

            // Başarılı giriş - denemeleri sıfırla
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;

            // İlk Giriş / Aktivasyon Kontrolü
            if (!user.IsActivated)
            {
                if (string.IsNullOrEmpty(licenseKey))
                    return new LoginResult { Success = false, Message = "Hesap henüz aktif değil. Lütfen lisans anahtarını giriniz.", NeedsActivation = true };

                if (user.LicenseKey != licenseKey)
                    return new LoginResult { Success = false, Message = "Geçersiz lisans anahtarı." };

                // Aktivasyon başarılı - Donanım profilini kaydet
                user.IsActivated = true;
                user.HardwareCpuId = currentProfile.CpuId;
                user.HardwareMotherboardId = currentProfile.MotherboardId;
                user.HardwareDiskId = currentProfile.DiskId;
                user.LicenseExpirationDate = DateTime.Now.AddYears(1);
            }
            else
            {
                // Cihaz Kontrolü (Toleranslı Hardware Match: 3 bileşenden en az 2'si eşleşmeli)
                int matchCount = 0;
                if (!string.IsNullOrEmpty(user.HardwareCpuId) && user.HardwareCpuId == currentProfile.CpuId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareMotherboardId) && user.HardwareMotherboardId == currentProfile.MotherboardId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareDiskId) && user.HardwareDiskId == currentProfile.DiskId) matchCount++;

                // Eğer eski sistemden kalan DeviceId varsa (gelişmiş sisteme geçiş yapıyorsak) onu kabul edip yeni sisteme güncelleyelim
                bool isLegacyMatch = !string.IsNullOrEmpty(user.DeviceId) && user.DeviceId == SecurityHelper.GetDeviceId();
                
                if (matchCount < 2 && !isLegacyMatch)
                {
                    return new LoginResult { Success = false, Message = "Bu hesap başka bir cihazda aktifleştirilmiş. Bu cihazda kullanılamaz." };
                }

                // Eğer Legacy match ile girdiyse veya eksik donanım ID'si varsa profil bilgilerini güncelle (Tolerance Self-Healing)
                if (isLegacyMatch || matchCount < 3)
                {
                    user.HardwareCpuId = currentProfile.CpuId;
                    user.HardwareMotherboardId = currentProfile.MotherboardId;
                    user.HardwareDiskId = currentProfile.DiskId;
                    user.DeviceId = null; // Eski sistemi temizle
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

            // Oturum ID'si oluştur
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
                await db.SaveChangesAsync();
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

        public static async System.Threading.Tasks.Task<bool> CheckAutoLoginAsync()
        {
            string? token = TokenStorage.GetToken();
            if (string.IsNullOrEmpty(token)) return false;

            using var db = new AppDbContext();
            
            var userTask = db.Users.FirstOrDefaultAsync(u => u.RememberMeToken == token && u.TokenExpiry > DateTime.Now);
            var hwProfileTask = SecurityHelper.GetHardwareProfileAsync();
            
            await System.Threading.Tasks.Task.WhenAll(userTask, hwProfileTask);
            var user = userTask.Result;
            var currentProfile = hwProfileTask.Result;

            if (user != null && user.IsActive)
            {
                // Cihaz Kontrolü (Toleranslı)
                int matchCount = 0;
                if (!string.IsNullOrEmpty(user.HardwareCpuId) && user.HardwareCpuId == currentProfile.CpuId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareMotherboardId) && user.HardwareMotherboardId == currentProfile.MotherboardId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareDiskId) && user.HardwareDiskId == currentProfile.DiskId) matchCount++;

                bool isLegacyMatch = !string.IsNullOrEmpty(user.DeviceId) && user.DeviceId == SecurityHelper.GetDeviceId();

                if (matchCount < 2 && !isLegacyMatch)
                {
                    TokenStorage.ClearToken();
                    return false;
                }

                if (isLegacyMatch || matchCount < 3)
                {
                    user.HardwareCpuId = currentProfile.CpuId;
                    user.HardwareMotherboardId = currentProfile.MotherboardId;
                    user.HardwareDiskId = currentProfile.DiskId;
                    user.DeviceId = null;
                }

                string newSessionId = SecurityHelper.GenerateToken();
                user.CurrentSessionId = newSessionId;
                user.LastLoginDate = DateTime.Now;
                await db.SaveChangesAsync();

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

        public static async System.Threading.Tasks.Task<bool> SendPasswordResetCodeAsync(string username, string licenseKey)
        {
            using var db = new AppDbContext();
            
            var userTask = db.Users.FirstOrDefaultAsync(u => u.Username == username && u.LicenseKey == licenseKey);
            var hwProfileTask = SecurityHelper.GetHardwareProfileAsync();
            
            await System.Threading.Tasks.Task.WhenAll(userTask, hwProfileTask);
            var user = userTask.Result;
            var currentProfile = hwProfileTask.Result;
            
            if (user == null || string.IsNullOrEmpty(user.Email)) return false;

            if (user.IsActivated)
            {
                int matchCount = 0;
                if (!string.IsNullOrEmpty(user.HardwareCpuId) && user.HardwareCpuId == currentProfile.CpuId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareMotherboardId) && user.HardwareMotherboardId == currentProfile.MotherboardId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareDiskId) && user.HardwareDiskId == currentProfile.DiskId) matchCount++;

                bool isLegacyMatch = !string.IsNullOrEmpty(user.DeviceId) && user.DeviceId == SecurityHelper.GetDeviceId();

                if (matchCount < 2 && !isLegacyMatch)
                {
                    return false;
                }
            }

            // 6 haneli kod oluştur
            string code = new Random().Next(100000, 999999).ToString();
            user.PasswordResetCode = code;
            user.PasswordResetCodeExpiry = DateTime.Now.AddMinutes(3);
            
            await db.SaveChangesAsync();

            // E-postayı gönder
            return await EmailService.SendPasswordResetCodeAsync(user.Email, code);
        }

        public static async System.Threading.Tasks.Task<bool> ResetPasswordAsync(string username, string licenseKey, string code, string newPassword)
        {
            using var db = new AppDbContext();
            
            var userTask = db.Users.FirstOrDefaultAsync(u => u.Username == username && u.LicenseKey == licenseKey);
            var hwProfileTask = SecurityHelper.GetHardwareProfileAsync();
            
            await System.Threading.Tasks.Task.WhenAll(userTask, hwProfileTask);
            var user = userTask.Result;
            var currentProfile = hwProfileTask.Result;
            
            if (user == null) return false;

            if (user.IsActivated)
            {
                int matchCount = 0;
                if (!string.IsNullOrEmpty(user.HardwareCpuId) && user.HardwareCpuId == currentProfile.CpuId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareMotherboardId) && user.HardwareMotherboardId == currentProfile.MotherboardId) matchCount++;
                if (!string.IsNullOrEmpty(user.HardwareDiskId) && user.HardwareDiskId == currentProfile.DiskId) matchCount++;

                bool isLegacyMatch = !string.IsNullOrEmpty(user.DeviceId) && user.DeviceId == SecurityHelper.GetDeviceId();

                if (matchCount < 2 && !isLegacyMatch)
                {
                    return false;
                }
            }

            // Kod ve Süre Kontrolü
            if (string.IsNullOrEmpty(user.PasswordResetCode) || 
                user.PasswordResetCode != code || 
                user.PasswordResetCodeExpiry < DateTime.Now)
            {
                return false; // Kod hatalı veya süresi geçmiş
            }

            user.PasswordHash = SecurityHelper.HashPassword(newPassword);
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            
            // Kullanılan kodu temizle
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiry = null;

            await db.SaveChangesAsync();
            return true;
        }

        public static string GenerateUniqueLicenseKey(AppDbContext db)
        {
            string key;
            do
            {
                // Format: XXXX-XXXX-XXXX-XXXX
                string raw = Guid.NewGuid().ToString("N").ToUpper();
                key = $"{raw.Substring(0, 4)}-{raw.Substring(4, 4)}-{raw.Substring(8, 4)}-{raw.Substring(12, 4)}";
            } while (db.Users.Any(u => u.LicenseKey == key));
            
            return key;
        }

        public static RegisterResult Register(string username, string dealerName, string email, string password, int agreementId)
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
                Email = email,
                PasswordHash = SecurityHelper.HashPassword(password),
                Role = "User",
                LicenseKey = GenerateUniqueLicenseKey(db),
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
