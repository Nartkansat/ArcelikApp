using ArcelikApp.Excel.Mapping.Profiles;
using System.Collections.Generic;
using System.Linq;

namespace ArcelikApp.Excel.Mapping
{
    /// <summary>
    /// Tüm WhiteGoods Excel profilleri burada kayıtlıdır.
    /// Yeni bir Excel tipi eklemek için buraya bir satır ekleyin.
    /// </summary>
    public static class ColumnMappingRegistry
    {
        private static readonly Dictionary<string, ColumnMappingProfile> _profiles =
            new(System.StringComparer.OrdinalIgnoreCase)
        {
            // --- Beyaz Eşya ---
            { "Ankastre",          AnkastreMappingProfile.Get()          },
            { "Soğutucu",          SogutucuMappingProfile.Get()          },
            { "Çamaşır Makinesi",  CamasirMakinesiMappingProfile.Get()   },
            { "Bulaşık Makinesi",  BulasikMakinesiMappingProfile.Get()   },
            { "Televizyon",        TvMappingProfile.Get()                },
            { "Klima",             KlimaMappingProfile.Get()             },
            { "Kurutma Makinesi",  KurutmaMappingProfile.Get()           },
            { "Solo Pişirici",     SoloPisiriciMappingProfile.Get()      },
            { "Isıtıcı Aletler",   IsiticiMappingProfile.Get()           },

            // --- KEA ---
            { "Mutfak",            KeaMutfakMappingProfile.Get()         },
            { "Süpürge ve Ütü",    KeaSupurgeUtuMappingProfile.Get()     },
        };

        private static readonly Dictionary<string, List<string>> _categoryMap = new(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Beyaz Eşya", new() { "Ankastre", "Soğutucu", "Çamaşır Makinesi", "Bulaşık Makinesi", "Televizyon", "Klima", "Kurutma Makinesi", "Solo Pişirici", "Isıtıcı Aletler" } },
            { "Kea",        new() { "Mutfak", "Süpürge ve Ütü" } }
        };

        public static ColumnMappingProfile? GetProfile(string fileTypeName)
            => _profiles.TryGetValue(fileTypeName, out var profile) ? profile : null;

        public static IEnumerable<string> GetTypesByCategory(string category)
            => _categoryMap.TryGetValue(category, out var types) ? types : Enumerable.Empty<string>();

        public static IEnumerable<string> GetAllFileTypeNames()
            => _profiles.Keys.ToList();
    }
}

