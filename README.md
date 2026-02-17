# ExchangeRateSolution

> Döviz kurlarını işlemek, depolamak ve belirli kaynaklardan alıp servisler aracılığıyla aktarmak üzere geliştirilmiş C# / .NET tabanlı çok katmanlı bir döviz kuru çözümü.

---

## 📌 Genel Bakış

**ExchangeRateSolution**, döviz kuru verileri ile çalışmak için hazırlanmış modüler ve katmanlı bir .NET çözümüdür. Proje; domain katmanı, veri erişim katmanı, servis katmanı ve Windows Service bileşeni içerir.

Amaç:

- Döviz kuru verilerini harici kaynaktan almak  
- Geçici (staging) alanlara yazmak  
- Ana tabloya güvenli şekilde aktarmak (merge)  
- Arka planda otomatik veri transferi yapmak  
- Katmanlı mimari ile sürdürülebilir yapı sağlamak  

---

## 🏗 Mimari Yaklaşım

Proje **katmanlı mimari** prensiplerine uygun olarak tasarlanmıştır:

- Sorumluluk ayrımı
- Test edilebilirlik
- Genişletilebilirlik
- Bağımlılıkların azaltılması

Domain modelleri ve arayüzler Core katmanında tutulur, veri erişimi ayrı katmanda yönetilir, iş kuralları servis katmanında uygulanır.

---
## ⚙️ Özellikler

- Çok katmanlı mimari yapı
- Domain entity tabanlı modelleme
- Repository / servis ayrımı
- Staging tablo kullanımı
- SQL MERGE ile kontrollü veri güncelleme
- Transaction destekli veri aktarımı
- Windows Service ile arka plan çalışması
- Toplu veri ekleme (bulk insert) desteğine uygun yapı

---

## 🗄 Veri Akışı Mantığı

1. Harici kaynaktan döviz verileri alınır  
2. Veriler staging tabloya yazılır  
3. SQL MERGE ile ana tablo güncellenir:
   - Yeni kayıt → insert
   - Değişen kayıt → update
   - Aynı kayıt → dokunulmaz
4. İşlem transaction içinde çalıştırılır  
5. Hata durumunda rollback yapılır  

---



