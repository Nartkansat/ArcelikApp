using System;

namespace ArcelikApp.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int? UserId { get; set; } // Null means send to all users
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
        
        // Navigation property
        public virtual User? User { get; set; }
    }
}
