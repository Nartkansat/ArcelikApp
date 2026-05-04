using System;

namespace ArcelikApp.Models
{
    public class Agreement
    {
        public int Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
