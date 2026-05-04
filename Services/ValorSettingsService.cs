using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ArcelikApp.Services
{
    /// <summary>
    /// Her beyaz eşya kategorisi için hangi valör (WholesalePrice30/60/90/120) 
    /// kullanılacağını kalıcı olarak saklar. Ayarlar AppData klasöründe JSON 
    /// dosyasına yazılır; uygulama her açılışında buradan okunur.
    /// </summary>
    public static class ValorSettingsService
    {
        // Desteklenen beyaz eşya kategorileri (ExcelFileType değerleri)
        public static readonly string[] WhiteGoodsCategories = new[]
        {
            "Soğutucu",
            "Çamaşır Makinesi",
            "Bulaşık Makinesi",
            "Ankastre",
            "Klima",
            "Isıtıcı Aletler",
            "Havalandırma",
            "Solo Pişirici",
            "Kurutma Makinesi",
            "Televizyon"
        };

        // Desteklenen valör seçenekleri
        public static readonly Dictionary<string, string> ValorOptions = new()
        {
            { "WholesalePrice30",  "30 Günlük"  },
            { "WholesalePrice60",  "60 Günlük"  },
            { "WholesalePrice90",  "90 Günlük"  },
            { "WholesalePrice120", "120 Günlük" },
            { "CashPrice",         "Peşin"      }
        };

        private static readonly string _settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Synctool");

        private static readonly string _settingsFile = Path.Combine(_settingsDir, "valor_settings.json");

        private static Dictionary<string, string> _cache = new();
        private static bool _loaded = false;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Bir kategori için kayıtlı valörü döner. Kayıt yoksa varsayılan 60 günlük döner.
        /// </summary>
        public static string GetValor(string category)
        {
            EnsureLoaded();
            return _cache.TryGetValue(category, out var valor) ? valor : "WholesalePrice60";
        }

        /// <summary>
        /// Bir kategori için valörü kaydeder (hem cache'e hem diske yazar).
        /// </summary>
        public static void SetValor(string category, string valorKey)
        {
            EnsureLoaded();
            _cache[category] = valorKey;
            Save();
        }

        /// <summary>
        /// Tüm kategori-valör eşlemesini döner (UI binding için).
        /// </summary>
        public static Dictionary<string, string> GetAll()
        {
            EnsureLoaded();

            // Tüm kategoriler için entry garantile
            var result = new Dictionary<string, string>();
            foreach (var cat in WhiteGoodsCategories)
                result[cat] = GetValor(cat);

            return result;
        }

        /// <summary>
        /// Birden fazla ayarı toplu olarak kaydeder.
        /// </summary>
        public static void SaveAll(Dictionary<string, string> settings)
        {
            EnsureLoaded();
            foreach (var kv in settings)
                _cache[kv.Key] = kv.Value;
            Save();
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            Load();
            _loaded = true;
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    string json = File.ReadAllText(_settingsFile);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null)
                        _cache = data;
                }
            }
            catch
            {
                // Bozuk dosya varsa görmezden gel, varsayılanlar kullanılır
                _cache = new Dictionary<string, string>();
            }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(_settingsDir);
                string json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
            }
            catch
            {
                // Diske yazma hatası sessizce geçilir (kritik değil)
            }
        }
    }
}
