
using ExchangeRate.Core.DTOs;
using ExchangeRate.Core.Entities;
using ExchangeRate.Data.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
namespace ExchangeRate.Service.Services
{
    public class ExchangeRateService
    {
        private readonly ExchangeRateRepository _repository;
        private readonly HttpClient _httpClient;

        public ExchangeRateService(ExchangeRateRepository repository, HttpClient httpClient)
        {
            _repository = repository;
            _httpClient = httpClient;
        }

        /// <summary>
        /// API'den döviz verilerini çeker ve DB'ye senkronize eder
        /// </summary>
        public async Task<bool> SyncExchangeRatesAsync()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Senkronizasyon başladı...");

                // 1. API'den veri çek (şimdilik mock data)
                var dtos = await FetchExchangeRatesFromApiAsync();

                if (dtos == null || !dtos.Any())
                {
                    Console.WriteLine("API'den veri gelmedi.");
                    return false;
                }

                // 2. DTO → Entity mapping
                var entities = MapDtosToEntities(dtos);

                // 3. Staging table'a bulk insert
                await _repository.BulkInsertToStagingAsync(entities);
                Console.WriteLine($"{entities.Count} kayıt staging'e eklendi.");

                // 4. MERGE ile ana tabloya aktar
                await _repository.MergeStagingToMainAsync();
                Console.WriteLine("MERGE işlemi tamamlandı.");

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Senkronizasyon başarılı!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] HATA: {ex.Message}");
                throw;
            }
        }

    
        private async Task<List<ExchangeRateDto>> FetchExchangeRatesFromApiAsync()
        {
            try
            {
                // 1. API'yi çağır
                string apiUrl = "https://api.exchangerate-api.com/v4/latest/USD";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API hatası: {response.StatusCode}");
                    return null;
                }

                // 2. JSON response'u oku
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 3. JSON'u deserialize et
                var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse == null || apiResponse.Rates == null)
                {
                    Console.WriteLine("API'den geçersiz yanıt.");
                    return null;
                }

                // 4. API response → DTO listesine dönüştür
                var dtoList = new List<ExchangeRateDto>();

                // Hangi para birimlerini almak istiyorsak onları seçelim
                var selectedCurrencies = new[] { "EUR", "GBP", "TRY", "JPY", "CHF" };

                foreach (var currency in selectedCurrencies)
                {
                    if (apiResponse.Rates.ContainsKey(currency))
                    {
                        // API USD bazlı kur veriyor, biz alış-satış simüle ediyoruz
                        decimal rate = apiResponse.Rates[currency];

                        dtoList.Add(new ExchangeRateDto
                        {
                            CurrencyCode = currency,
                            RateBuy = rate * 0.98m,      // %2 düşük (alış)
                            RateSell = rate * 1.02m,     // %2 yüksek (satış)
                            RateAverage = rate,          // Ortalama
                            Date = DateTime.Parse(apiResponse.Date)
                        });
                    }
                }

                Console.WriteLine($"API'den {dtoList.Count} para birimi çekildi.");
                return dtoList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API çağrısı hatası: {ex.Message}");
                return null;
            }
        }
        

   
        private List<Core.Entities.ExchangeRate> MapDtosToEntities(List<ExchangeRateDto> dtos)
        {
            var entities = new List<Core.Entities.ExchangeRate>();

            foreach (var dto in dtos)
            {
                entities.Add(new Core.Entities.ExchangeRate
                {
                    CurrencyCode = dto.CurrencyCode,
                    RateBuy = dto.RateBuy,
                    RateSell = dto.RateSell,
                    RateAverage = dto.RateAverage,
                    Date = dto.Date,
                    CreatedAt = DateTime.Now
                });
            }

            return entities;
        }
    }
}