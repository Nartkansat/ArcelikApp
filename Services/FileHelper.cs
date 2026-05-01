using System;
using System.IO;

namespace ArcelikApp.Services
{
    public static class FileHelper
    {
        private const string StorageFolderName = "Storage";

        /// <summary>
        /// Uygulama dizini içinde depolama klasörünü oluşturur ve yolunu döndürür.
        /// </summary>
        public static string GetStorageDirectory()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StorageFolderName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        /// <summary>
        /// Seçilen dosyayı uygulama içindeki Storage klasörüne kopyalar ve relative (göreli) yolu döndürür.
        /// </summary>
        public static string CopyToStorage(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                return sourceFilePath;

            string fileName = Path.GetFileName(sourceFilePath);
            string destinationPath = Path.Combine(GetStorageDirectory(), fileName);

            // Dosyayı kopyala (varsa üzerine yaz)
            File.Copy(sourceFilePath, destinationPath, true);

            // Veritabanına kaydedilecek dinamik yol: "Storage\dosya.xlsx"
            return Path.Combine(StorageFolderName, fileName);
        }

        /// <summary>
        /// Veritabanından gelen yolu tam yola dönüştürür.
        /// </summary>
        public static string GetAbsolutePath(string savedPath)
        {
            if (string.IsNullOrEmpty(savedPath)) return string.Empty;

            // Eğer yol zaten tam yolsa (C:\... gibi - eski kayıtlar için) direkt döndür
            if (Path.IsPathRooted(savedPath))
            {
                return savedPath;
            }

            // Değilse uygulama çalıştığı dizinle birleştir
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, savedPath);
        }

        /// <summary>
        /// Dosyanın başka bir işlem tarafından kilitli olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsFileLocked(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // Dosya kilitli (başka bir uygulama tarafından kullanılıyor)
                return true;
            }
            return false;
        }
    }
}
