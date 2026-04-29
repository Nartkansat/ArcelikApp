using System;

namespace ArcelikApp.Models
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string UploadDate { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Kea, BeyazEşya, Oliz
        public string FilePath { get; set; } = string.Empty;
        public byte[]? FileData { get; set; }
    }
}
