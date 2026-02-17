using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ExchangeRate.Data.Data.Repositories
{
    public class ExchangeRateRepository : IRepository<ExchangeRate.Core.Entities.ExchangeRate>
    {
        private readonly string _connectionString;

        public ExchangeRateRepository(string connectionString)
        {
            _connectionString = connectionString;
        }


        public async Task BulkInsertToStagingAsync(List<Core.Entities.ExchangeRate> entities)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. DataTable oluştur
                        var dataTable = new DataTable();

                        // 2. Kolonları ekle (staging table ile aynı sırada)
                        dataTable.Columns.Add("CurrencyCode", typeof(string));
                        dataTable.Columns.Add("RateBuy", typeof(decimal));
                        dataTable.Columns.Add("RateSell", typeof(decimal));
                        dataTable.Columns.Add("RateAverage", typeof(decimal));
                        dataTable.Columns.Add("Date", typeof(DateTime));
                        dataTable.Columns.Add("CreateAt", typeof(DateTime));

                        // 3. rates listesini DataTable'a dönüştür
                        foreach (var rate in entities)
                        {
                            dataTable.Rows.Add(
                                rate.CurrencyCode,
                                rate.RateBuy,
                                rate.RateSell,
                                rate.RateAverage,
                                rate.Date,
                                rate.CreatedAt
                            );
                        }

                        // 4. SqlBulkCopy ile staging table'a yaz
                        using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "ExchangeRate_Staging";
                            bulkCopy.ColumnMappings.Add("CurrencyCode", "CurrencyCode");
                            bulkCopy.ColumnMappings.Add("RateBuy", "RateBuy");
                            bulkCopy.ColumnMappings.Add("RateSell", "RateSell");
                            bulkCopy.ColumnMappings.Add("RateAverage", "RateAverage");
                            bulkCopy.ColumnMappings.Add("Date", "Date");
                            bulkCopy.ColumnMappings.Add("CreateAt", "CreateAt");

                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        // Başarılı → commit
                        transaction.Commit();
                    }
                    catch
                    {
                        // Hata → rollback
                        transaction.Rollback();
                        throw; // Hatayı yukarı fırlat
                    }
                }
            }
        }



        public async Task DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "DELETE FROM ExchangeRate WHERE Id=@Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    await command.ExecuteNonQueryAsync();
                }
            }

        }

        public async Task<List<Core.Entities.ExchangeRate>> GetAllAsync()
        {
            var list = new List<Core.Entities.ExchangeRate>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM ExchangeRate";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new Core.Entities.ExchangeRate
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                CurrencyCode = reader.GetString(reader.GetOrdinal("CurrencyCode")),
                                RateBuy = reader.GetDecimal(reader.GetOrdinal("RateBuy")),
                                RateSell = reader.GetDecimal(reader.GetOrdinal("RateSell")),
                                RateAverage = reader.GetDecimal(reader.GetOrdinal("RateAverage")),
                                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                                                    ? (DateTime?)null
                                             : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))



                            });
                        }
                    }
                }
            }
            return list;

        }

        public async Task<Core.Entities.ExchangeRate> GetByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM ExchangeRate WHERE Id=@Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Core.Entities.ExchangeRate
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                CurrencyCode = reader.GetString(reader.GetOrdinal("CurrencyCode")),
                                RateBuy = reader.GetDecimal(reader.GetOrdinal("RateBuy")),
                                RateSell = reader.GetDecimal(reader.GetOrdinal("RateSell")),
                                RateAverage = reader.GetDecimal(reader.GetOrdinal("RateAverage")),
                                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))


                            };

                        }
                    }

                }
            }
            return null;  //Buılamazsa null dön
        }

        //public async Task MergeStagingToMainAsync() // Staging tablodaki verileri ana tabloya MERGE ile aktaran async metot
        //{
        //    using (var connection = new SqlConnection(_connectionString)) // Connection string ile SQL bağlantısı oluşturur
        //    {
        //        await connection.OpenAsync(); // Veritabanı bağlantısını asenkron olarak açar

        //        using (var transaction = connection.BeginTransaction()) // Tüm işlemleri tek transaction içinde başlatır
        //        {
        //            try // Hata olasılığı olan veritabanı işlemlerini güvenli blokta çalıştırır
        //            {
        //                var mergeQuery = @"MERGE INTO ExchangeRate AS Target USING ExchangeRate_Staging AS Source  
        //        ON(Target.CurrencyCode = Source.CurrencyCode AND Target.Date = Source.Date) 
        //        -- Hedef ve staging tablolarını CurrencyCode ve Date alanlarına göre eşleştirir

        //        WHEN MATCHED THEN
        //        -- Eşleşen kayıt varsa update işlemi yapar
        //        UPDATE SET
        //            Target.RateBuy = Source.RateBuy,       -- Alış kurunu staging değerine günceller
        //            Target.RateSell = Source.RateSell,     -- Satış kurunu staging değerine günceller
        //            Target.RateAverage = Source.RateAverage, -- Ortalama kuru staging değerine günceller
        //            Target.UpdatedAt = GETDATE()          -- Güncellenme zamanını şu anki tarih yapar

        //        WHEN NOT MATCHED THEN
        //        -- Eşleşen kayıt yoksa insert işlemi yapar
        //        INSERT (CurrencyCode, RateBuy, RateSell, RateAverage, Date, CreatedAt) 
        //        VALUES (Source.CurrencyCode, Source.RateBuy, Source.RateSell, Source.RateAverage, Source.Date, Source.CreateAt);
        //        -- Staging tablodaki değerleri ana tabloya yeni kayıt olarak ekler
        //        ";

        //                using (var command = new SqlCommand(mergeQuery, connection, transaction)) // MERGE sorgusu için komut nesnesi oluşturur
        //                {
        //                    await command.ExecuteNonQueryAsync(); // Sorguyu asenkron olarak çalıştırır ve etkilenen satırları uygular
        //                }

        //                transaction.Commit(); // Tüm işlemler başarılıysa transaction’ı kalıcı hale getirir
        //            }
        //            catch // Herhangi bir hata oluşursa bu blok çalışır
        //            {
        //                transaction.Rollback(); // Hata durumunda yapılan tüm değişiklikleri geri alır
        //                throw; // Hatayı üst katmana tekrar fırlatır
        //            }
        //        }
        //    }
        //}
        public async Task MergeStagingToMainAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var mergeQuery = @"
MERGE INTO ExchangeRate AS Target
USING (
    SELECT
        CurrencyCode,
        Date,
        MAX(RateBuy)     AS RateBuy,
        MAX(RateSell)    AS RateSell,
        MAX(RateAverage) AS RateAverage,
        MAX(CreateAt)    AS CreateAt
    FROM ExchangeRate_Staging
    GROUP BY CurrencyCode, Date
) AS Source
ON Target.CurrencyCode = Source.CurrencyCode
AND Target.Date = Source.Date

WHEN MATCHED AND (
       Target.RateBuy <> Source.RateBuy
    OR Target.RateSell <> Source.RateSell
    OR Target.RateAverage <> Source.RateAverage
)
THEN UPDATE SET
    Target.RateBuy = Source.RateBuy,
    Target.RateSell = Source.RateSell,
    Target.RateAverage = Source.RateAverage,
    Target.UpdatedAt = GETDATE()

WHEN NOT MATCHED THEN
INSERT (
    CurrencyCode,
    RateBuy,
    RateSell,
    RateAverage,
    Date,
    CreatedAt   -- main tabloda bu doğru isim olmalı
)
VALUES (
    Source.CurrencyCode,
    Source.RateBuy,
    Source.RateSell,
    Source.RateAverage,
    Source.Date,
    Source.CreateAt
);";

                        using (var command = new SqlCommand(mergeQuery, connection, transaction))
                        {
                            command.CommandTimeout = 120;
                            await command.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }




        public async Task UpdateAsync(Core.Entities.ExchangeRate entity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE ExchangeRate 
                      SET CurrencyCode = @CurrencyCode,
                          RateBuy = @RateBuy,
                          RateSell = @RateSell,
                          RateAverage = @RateAverage,
                          Date = @Date,
                          UpdatedAt = @UpdatedAt
                      WHERE Id = @Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", entity.Id);
                    command.Parameters.AddWithValue("@CurrencyCode", entity.CurrencyCode);
                    command.Parameters.AddWithValue("@RateBuy", entity.RateBuy);
                    command.Parameters.AddWithValue("@RateSell", entity.RateSell);
                    command.Parameters.AddWithValue("@RateAverage", entity.RateAverage);
                    command.Parameters.AddWithValue("@Date", entity.Date);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now); // Güncelleme zamanı

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        
    }
}
