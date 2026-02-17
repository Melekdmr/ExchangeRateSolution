using ExchangeRate.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeTransferWinService.Data
{
 
        // DB’den servis ayarlarını okuyan repository sınıfı
        public class ServiceSettingsRepository
        {
            // Connection string’i tutan private readonly alan
            private readonly string _conn;

            // Constructor — dışarıdan connection string alır
            public ServiceSettingsRepository(string conn)
            {
                _conn = conn; // gelen connection string saklanır
            }

            // Servis ayarlarını DB’den okuyup dönen metod
            public ServiceSettings GetSettings()
            {
                // Döndürülecek model nesnesi oluşturulur
                var result = new ServiceSettings();

                // SqlConnection oluştur — using → iş bitince otomatik dispose
                using (var conn = new SqlConnection(_conn))

                // SQL komutu oluştur — TOP 1 → ilk kaydı al
                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 Id, IntervalMinutes, IsActive, CreatedAt, UpdatedAt \r\nFROM ServiceSettings \r\nORDER BY Id DESC",
                    conn))
                {
                    // DB bağlantısını aç
                    conn.Open();

                    // Komutu çalıştır → DataReader döner
                    using (var r = cmd.ExecuteReader())
                    {
                        // Kayıt var mı kontrol et
                        if (r.Read())
                        {
                            // IntervalMinutes kolonunu int olarak modele ata
                            result.IntervalMinutes = (int)r["IntervalMinutes"];

                            // IsActive kolonunu bool olarak modele ata
                            result.IsActive = (bool)r["IsActive"];
                        result.CreatedAt = (DateTime)r["CreatedAt"];

                        // UpdatedAt kolonunu DateTime? olarak modele ata (nullable)
                        result.UpdatedAt = r["UpdatedAt"] == DBNull.Value
                                           ? (DateTime?)null
                                           : (DateTime)r["UpdatedAt"];
                    }
                    }
                }

                // Doldurulmuş ayar nesnesini geri döndür
                return result;
            }
        }
    }


