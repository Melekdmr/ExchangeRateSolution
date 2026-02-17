using ExchangeRate.Service.Services;
using ExchangeRate.Data.Data.Repositories;
using ExchangeTransferWinService.Data;
using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace ExchangeTransferWinService
{
    public partial class ExchangeRateWorkerService : ServiceBase
    {
        private Timer _timer;
        private ExchangeRateService _exchangeRateService;
        private ServiceSettingsRepository _settingsRepository;
        private string _connectionString;
        private bool _isRunning = false;
        private HttpClient _httpClient;

        public ExchangeRateWorkerService()
        {
            InitializeComponent();
            ServiceName = "ExchangeRateWorkerService";
        }

        /// <summary>
        /// Servis başlatıldığında çalışır
        /// </summary>
        protected override void OnStart(string[] args)
        {
            try
            {
                // 1. Connection string'i App.config'den oku
                _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                // 2. Repository ve Service oluştur
                var exchangeRateRepository = new ExchangeRateRepository(_connectionString);
                _httpClient = new HttpClient();
                _exchangeRateService = new ExchangeRateService(exchangeRateRepository, _httpClient);
                _settingsRepository = new ServiceSettingsRepository(_connectionString);

                // 3. İlk ayarları oku
                var settings = _settingsRepository.GetSettings();

                if (!settings.IsActive)
                {
                    LogToFile("Servis pasif durumda, başlatılmadı.");
                    Stop();
                    return;
                }

                // 4. Timer'ı başlat (interval dakika → milisaniye)
                int intervalMs = settings.IntervalMinutes * 60 * 1000;
                _timer = new Timer(OnTimerElapsed, null, 0, intervalMs);

                LogToFile($"Servis başlatıldı. Interval: {settings.IntervalMinutes} dakika");
            }
            catch (Exception ex)
            {
                LogToFile($"Servis başlatma hatası: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Timer her tetiklendiğinde çalışır
        /// </summary>
        private async void OnTimerElapsed(object state)
        {
            // Eşzamanlı çalışmayı engelle
            if (_isRunning)
            {
                LogToFile("Önceki işlem devam ediyor, atlandı.");
                return;
            }

            _isRunning = true;

            try
            {
                // 1. Ayarları yeniden oku (runtime değişiklik için)
                var settings = _settingsRepository.GetSettings();

                if (!settings.IsActive)
                {
                    LogToFile("Servis pasif duruma alındı, işlem yapılmadı.");
                    _isRunning = false;
                    return;
                }

                // 2. Interval değişmişse timer'ı güncelle
                int newIntervalMs = settings.IntervalMinutes * 60 * 1000;
                _timer.Change(0, newIntervalMs);

                // 3. Senkronizasyon işlemini çalıştır
                LogToFile("Senkronizasyon başladı...");
                bool success = await _exchangeRateService.SyncExchangeRatesAsync();

                if (success)
                {
                    LogToFile("Senkronizasyon başarılı.");
                }
                else
                {
                    LogToFile("Senkronizasyon başarısız.");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Timer hatası: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
            }
        }

        /// <summary>
        /// Servis durdurulduğunda çalışır
        /// </summary>
        protected override void OnStop()
        {
            _timer?.Dispose();
            _httpClient?.Dispose();
            LogToFile("Servis durduruldu.");
        }

        /// <summary>
        /// Log dosyasına yazar
        /// </summary>
        private void LogToFile(string message)
        {
            try
            {
                string logPath = @"C:\Logs\ExchangeRateService.log";
                string logDir = System.IO.Path.GetDirectoryName(logPath);

                if (!System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }

                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                System.IO.File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // Log hatası sessizce geçilir
            }
        }
    }
}