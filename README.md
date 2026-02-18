# 💱 Exchange Rate Solution

> Multi-layered .NET solution for fetching, processing, and synchronizing exchange rate data using staging tables and SQL MERGE

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2%2B-blue.svg)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2020%2B-red.svg)](https://www.microsoft.com/sql-server)
[![Architecture](https://img.shields.io/badge/Architecture-Layered-green.svg)](#)

## 🎯 Overview

**ExchangeRateSolution** is a modular, layered .NET solution designed for working with exchange rate data. The project includes domain layer, data access layer, service layer, and Windows Service component.

### Key Goals
- Fetch exchange rate data from external sources
- Write to staging tables for validation
- Safely transfer to main tables using MERGE
- Automated background data synchronization
- Maintainable layered architecture

## 🏗️ Architecture
```
┌─────────────────────────────────────────────────────┐
│         ExchangeTransferWinService                  │
│         (Windows Service - Timer)                   │
└──────────────────┬──────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────┐
│         ExchangeRate.Service                        │
│         (Business Logic)                            │
└──────────────────┬──────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────┐
│         ExchangeRate.Data                           │
│         (Repository Pattern)                        │
└──────────────────┬──────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────┐
│         ExchangeRate.Core                           │
│         (Domain Models & Interfaces)                │
└─────────────────────────────────────────────────────┘
```

## ✨ Features

- ✅ **Layered Architecture** - Clean separation of concerns
- ✅ **Repository Pattern** - Abstracted data access
- ✅ **Staging Tables** - Safe data validation before merge
- ✅ **SQL MERGE** - Automatic INSERT/UPDATE logic
- ✅ **Transaction Support** - Rollback on errors
- ✅ **Bulk Operations** - High-performance data transfer
- ✅ **Windows Service** - Automated background processing
- ✅ **Testable** - Dependency injection ready

## 📦 Project Structure
```
ExchangeRateSolution/
│
├── ExchangeRate.Core/              # Domain Layer
│   ├── Entities/                   # Domain models
│   ├── Interfaces/                 # Repository interfaces
│   └── DTOs/                       # Data transfer objects
│
├── ExchangeRate.Data/              # Data Access Layer
│   ├── Repositories/               # Repository implementations
│   ├── Context/                    # Database context
│   └── Migrations/                 # Database migrations
│
├── ExchangeRate.Service/           # Service Layer
│   ├── Services/                   # Business logic
│   ├── Validators/                 # Data validation
│   └── Mappers/                    # Object mapping
│
└── ExchangeTransferWinService/     # Windows Service
    ├── Worker.cs                   # Service worker
    └── App.config                  # Configuration
```

## 🔄 Data Flow
```
External API
    ↓
[Fetch Rates]
    ↓
ExchangeRate_Staging (Temp)
    ↓
[Validation]
    ↓
[SQL MERGE]
    ↓
ExchangeRate (Main Table)
```

### MERGE Logic
```sql
MERGE ExchangeRate AS target
USING ExchangeRate_Staging AS source
ON target.CurrencyCode = source.CurrencyCode 
   AND target.RateDate = source.RateDate

WHEN MATCHED AND target.Rate <> source.Rate THEN
    UPDATE SET 
        target.Rate = source.Rate,
        target.UpdatedAt = GETDATE()

WHEN NOT MATCHED BY TARGET THEN
    INSERT (CurrencyCode, Rate, RateDate, CreatedAt)
    VALUES (source.CurrencyCode, source.Rate, source.RateDate, GETDATE());
```

## 🚀 Installation

### Prerequisites
- .NET Framework 4.7.2+
- SQL Server 2016+
- Visual Studio 2019+
- Administrator privileges (for Windows Service)

### 1. Database Setup
```sql
-- Create main table
CREATE TABLE ExchangeRate (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CurrencyCode NVARCHAR(3) NOT NULL,
    Rate DECIMAL(18,6) NOT NULL,
    RateDate DATE NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT UQ_Currency_Date UNIQUE (CurrencyCode, RateDate)
)

-- Create staging table
CREATE TABLE ExchangeRate_Staging (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CurrencyCode NVARCHAR(3) NOT NULL,
    Rate DECIMAL(18,6) NOT NULL,
    RateDate DATE NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
)

-- Service settings
CREATE TABLE ServiceSettings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    IntervalMinutes INT NOT NULL,
    IsActive BIT NOT NULL,
    ApiUrl NVARCHAR(500)
)

INSERT INTO ServiceSettings VALUES (60, 1, 'https://api.exchangerate.com/rates')
```

### 2. Configuration

Edit `App.config`:
```xml
<connectionStrings>
  <add name="ExchangeRateDb" 
       connectionString="Server=SERVER;Database=ExchangeRateDB;Integrated Security=true;" />
</connectionStrings>

<appSettings>
  <add key="ApiUrl" value="https://api.exchangerate.com/rates" />
  <add key="ApiKey" value="YOUR_API_KEY" />
</appSettings>
```

### 3. Build & Deploy
```cmd
# Build solution
msbuild ExchangeRateSolution.sln /p:Configuration=Release

# Copy to service directory
xcopy "ExchangeTransferWinService\bin\Release\*.*" "C:\Services\ExchangeRateService\" /Y
```

### 4. Install Windows Service
```cmd
# Run CMD as Administrator
sc create ExchangeRateService binPath="C:\Services\ExchangeRateService\ExchangeTransferWinService.exe" start=auto
sc start ExchangeRateService
```

## 💻 Usage

### Service Management
```cmd
# Check status
sc query ExchangeRateService

# Start/Stop
sc start ExchangeRateService
sc stop ExchangeRateService

# View logs
notepad C:\Services\ExchangeRateService\logs\log_YYYYMMDD.txt
```

### Manual Trigger (For Testing)
```csharp
var service = new ExchangeRateService();
service.FetchAndSyncRates();
```

## 🧪 Testing
```sql
-- Check staging table
SELECT * FROM ExchangeRate_Staging ORDER BY CreatedAt DESC

-- Check main table
SELECT * FROM ExchangeRate ORDER BY RateDate DESC, CurrencyCode

-- Verify sync
SELECT 
    CurrencyCode,
    Rate,
    RateDate,
    UpdatedAt
FROM ExchangeRate
WHERE RateDate = CAST(GETDATE() AS DATE)
```

## ⚙️ Configuration Options
```sql
-- Change fetch interval (2 hours)
UPDATE ServiceSettings SET IntervalMinutes = 120

-- Pause service
UPDATE ServiceSettings SET IsActive = 0

-- Resume service
UPDATE ServiceSettings SET IsActive = 1

-- Update API URL
UPDATE ServiceSettings SET ApiUrl = 'https://new-api.com/rates'
```

## 🔧 Troubleshooting

### Service Won't Start
```cmd
eventvwr.msc → Application logs
```

### API Connection Failed
- Check `ApiUrl` and `ApiKey` in config
- Verify network connectivity
- Check API rate limits

### Data Not Syncing
```sql
-- Check staging table
SELECT COUNT(*) FROM ExchangeRate_Staging

-- Check for errors in logs
-- Review transaction rollbacks
```

### Duplicate Key Error
```sql
-- Check existing records
SELECT CurrencyCode, RateDate, COUNT(*) 
FROM ExchangeRate 
GROUP BY CurrencyCode, RateDate 
HAVING COUNT(*) > 1
```


## 🔐 Security

- ✅ API keys in configuration (not hardcoded)
- ✅ SQL parameterized queries (no injection risk)
- ✅ Least privilege database access
- ✅ Transaction rollback on errors
- ✅ Encrypted connection strings (optional)




## 📞 Contact

- GitHub: [@Melekdmr](https://github.com/Melekdmr)
- Issues: [Report a bug](https://github.com/Melekdmr/ExchangeRateSolution/issues)

---

