using System;

namespace ArcelikApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // "Admin" or "User"
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime? LicenseExpirationDate { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public bool IsActivated { get; set; } = false;
        public string? DeviceId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // --- Oturum Takibi ---
        /// <summary>Kullanıcının şu anki oturum ID'si. Eğer bir kullanıcı girerse bu güncellenir. Başka bir yerden girilirse bu ID geçersiz kalır.</summary>
        public string? CurrentSessionId { get; set; }
        public DateTime? LastLoginDate { get; set; }

        // --- Beni Hatırla ---
        public string? RememberMeToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
    }
}
