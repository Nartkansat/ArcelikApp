using System;

namespace ArcelikApp.Models
{
    public class UserConsent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AgreementId { get; set; }
        public DateTime AcceptedAt { get; set; } = DateTime.Now;
        public string IpAddress { get; set; } = string.Empty;
        
        public User? User { get; set; }
        public Agreement? Agreement { get; set; }
    }
}
