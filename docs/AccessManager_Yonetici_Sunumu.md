# Access Manager - Yonetici Sunum Dokumani

> **Belge Amaci:** Bu dokuman, Access Manager'in is degerini, teknik olgunlugunu ve yayginlastirma planini yonetim seviyesinde net ve olculebilir bicimde sunmak icin hazirlanmistir.

---

## Kapak Bilgileri

| Alan | Icerik |
|---|---|
| Urun | `arabam.com Access Manager` |
| Sunum Tipi | Yonetici Degerlendirme ve Yol Haritasi |
| Hedef Kitle | Ust Yonetim, IT Liderligi, Operasyon Yonetimi |
| Surum | `v1.0` |
| Tarih | `[Sunum tarihi buraya]` |
| Hazirlayan | `[Takim / Kisi]` |

---

## Icindekiler
1. Yonetici Ozeti  
2. Is Problemi ve Firsat  
3. Cozum Kapsami (Urun Modulleri)  
4. Is Degeri ve Beklenen Etki  
5. Son Donem Gelistirmeleri  
6. Unvan Sablonlari (Neden eklendi?)  
7. Teknik Mimari (Yuksek Seviye)  
8. Guvenlik, Isletim ve Uyum  
9. AI Yapisi: Kac Agent Var, Nasil Calisiyor  
10. Basari Metrikleri (KPI)  
11. Riskler ve Aksiyon Plani  
12. Yol Haritasi  
13. Demo Akisi  
14. 2 Dakikalik Kapanis Metni

---

## 1) Yonetici Ozeti

- Access Manager, erisim ve yetki sureclerini merkezi hale getirerek operasyonel hizi artirir.
- Ise giris/isten cikis, zimmet, rol ve uygulama erisimleri tek platformda izlenebilir hale gelir.
- AI destekli read-only SQL akisi ile veri sorularina kontrollu ve guvenli yanit uretilir.
- Mimari sade, bakimi kolay ve olceklenebilir bir yapi uzerine kuruludur.

**Beklenen yonetsel kazanım:** Daha hizli operasyon + daha dusuk hata orani + daha iyi denetlenebilirlik.

---

## 2) Is Problemi ve Firsat

### Mevcut Problemler
- Yetki ve erisim surecleri ekipler arasinda farkli standartlarla ilerliyor.
- Manuel surecler gecikme ve insan hatasi uretiyor.
- Zimmet ve erisim kayitlari daginik oldugu icin denetim zorlasiyor.
- Kurumsal olcek buyudukce yonetim karmasikligi artis gosteriyor.

### Firsat
- Surecleri teklestirerek operasyon maliyetini dusurmek
- Standart karar mekanizmalariyla kaliteyi artirmak
- Yonetim ve denetim icin tek gercek kaynagi (single source of truth) saglamak

---

## 3) Cozum Kapsami (Urun Modulleri)

| Moduller | Islev |
|---|---|
| Kontrol Paneli | Genel durum ve operasyon ozeti |
| Personel | Personel kayit ve yasam dongusu yonetimi |
| Departmanlar / Uygulamalar | Organizasyon ve sistem modelleme |
| Donanim & Zimmet | Varlik atama ve iade sureci |
| Roller & Yetkiler | Standart izin politikasi |
| Ise Giris / Isten Cikis | Kritik gecis sureclerinin standartlasmasi |
| Yetki Talepleri | Talep, onay ve izlenebilirlik |
| Raporlar / Log | Gecmise donuk denetim ve analiz |
| Unvan Sablonlari | Ise giriste unvan standardizasyonu |
| arabam AI | Operasyonel yardim + read-only SQL |

---

## 4) Is Degeri ve Beklenen Etki

| Boyut | Etki |
|---|---|
| Hiz | Onboarding/offboarding suresinde azalma |
| Kalite | Rol/izin atamalarinda tutarlilik |
| Uyum | Karar izinin saklanmasi ve denetim kolayligi |
| Gorunurluk | Log ve raporlarla tam izlenebilirlik |
| Olceklenebilirlik | Ekip/sistem artisinda merkezi kontrol |

**Yonetim Mesaji:** Sistem yalnizca teknik bir arac degil, operasyonel verimlilik platformudur.

---

## 5) Son Donem Gelistirmeleri

### AI - Salt Okunur SQL Akisi
- `propose_sql`: SQL onerir, onay bekler
- `execute_pending_sql`: yalnizca onayli bekleyen SQL'i calistirir

### Guvenlik Sinirlari
- Sadece SELECT/WITH...SELECT
- Satir limiti (max 1000)
- Timeout ve cikti boyut korumasi
- Konusma bazli bekleyen sorgu modeli

### UI Iyilestirmeleri
- SQL onay/onaylamama butonlari
- SQL kod blogu gorunurluk optimizasyonu
- Tablo sonuc formatlama iyilestirmeleri

---

## 6) Unvan Sablonlari Neden Eklendi?

### Is Gerekcesi
- Ise giriste unvan alanini standartlastirmak
- Departman + ekip + seviye baglamina uygun otomatik oneri sunmak

### Operasyonel Esneklik
- Seviye alani serbest metinle ozellestirilebilir
- Sablonlar sonradan guncellenebilir / silinebilir
- Standart + esnek model birlikte korunur

---

## 7) Teknik Mimari (Yuksek Seviye)

| Katman | Sorumluluk |
|---|---|
| `AccessManager.Web` | UI, Controller, orkestrasyon |
| `AccessManager.Application` | Arayuzler ve uygulama kurallari |
| `AccessManager.Infrastructure` | Repository, dis servis entegrasyonlari, DB erisimi |
| `AccessManager.Domain` | Temel is modeli/entity katmani |

- Platform: ASP.NET Core MVC
- Veritabani: PostgreSQL (Npgsql/Dapper tabanli)
- DI yonetimi: `ServiceCollectionExtensions`

---

## 8) Guvenlik, Isletim ve Uyum

- Global kimlik dogrulama zorunlulugu (authorize policy)
- Cookie auth + rol bazli yetkilendirme
- Hata/kritik olaylarin genisletilmis log kaydi
- Hosted service ile periyodik bakim gorevleri
- Canli ortamda repo/path dogrulama ve isletim kontrolleri

---

## 9) AI Yapisi: Kac Agent Var, Nasil Calisiyor?

### Kac Agent?
- **Mevcutta 1 aktif uygulama agent'i:** `AiChatService`

### Nasil Calisiyor?
1. Kullanici mesaji gelir
2. Konusma servisi yetki/konusma baglamini dogrular
3. Agent proje baglamini toplar (yapi + vektor parcalari)
4. Model gerekirse tool cagirir
5. Tool sonucu modele geri verilir
6. Nihai yanit olusur, DB'ye kaydedilir, UI'a stream edilir

### Tool Gruplari
- Dosya: `read_file`, `write_file`, `apply_diff`
- Build/Git: `run_build`, `confirm_and_push`, `create_pr`, `git_commit_and_push`
- SQL: `propose_sql`, `execute_pending_sql`

### Neden Tek Agent + Tool?
- Daha ongorulebilir davranis
- Guvenlik ve audit kontrolunun kolaylasmasi
- Operasyonel bakim maliyetinin dusmesi

---

## 10) Basari Metrikleri (KPI)

| KPI | Mevcut | Hedef | Not |
|---|---:|---:|---|
| Ise giris tamamlama suresi | `[X]` | `[Y]` | Saat/Dakika |
| Offboarding SLA uyumu | `[%X]` | `[%Y]` | Gecikme azalimi |
| Manuel yetki duzeltme orani | `[%X]` | `[%Y]` | Kalite etkisi |
| Denetim talebi cevap suresi | `[X]` | `[Y]` | Izlenebilirlik etkisi |
| Aylik aktif yonetilen kayit | `[X]` | `[Y]` | Buyume takibi |

> Not: Bu tablo sunum oncesi canli verilerle doldurulmalidir.

---

## 11) Riskler ve Aksiyon Plani

| Risk | Etki | Olasilik | Aksiyon |
|---|---|---|---|
| Veri kalite farkliliklari | Yuksek | Orta | Zorunlu alan ve dogrulama kurallari |
| Surec degisimine adaptasyon | Orta | Orta | Egitim + kilavuz + pilot ekip |
| Entegrasyon bagimliliklari | Orta | Dusuk/Orta | Fazli rollout ve geri donus planlari |
| AI yanlis yorum riski | Orta | Dusuk | Read-only, onayli SQL modeli ve prompt guvenlikleri |

---

## 12) Yol Haritasi

### Kisa Vade (0-3 Ay)
- Dashboard KPI kartlari
- Gelismis filtreleme + export
- Kritik surecler icin isletim metrikleri

### Orta Vade (3-6 Ay)
- Onay akislarinda kural motoru
- AI icin schema-ozet yardim araci
- Yetki taleplerinde daha guclu otomasyon

### Uzun Vade (6+ Ay)
- Ticketing/identity sistemleri ile entegrasyonlar
- Kurumsal raporlama katmaninin genisletilmesi

---

## 13) Demo Akisi (Toplanti Icin)

1. Kontrol paneli genel gorunum
2. Onboarding ile yeni personel olusturma
3. Rol/yetki ve zimmet atama
4. Unvan sablonu ekleme/guncelleme
5. AI ile read-only SQL onayli sorgu
6. Log ve rapor ekranlarinda izlenebilirlik

---

## 14) 2 Dakikalik Kapanis Metni (Hazir Konusma)

"Access Manager ile hedefimiz, kurum icindeki erisim ve yetki sureclerini kisiye bagli olmaktan cikarip standart, olculebilir ve denetlenebilir bir yapıya tasimak. Bugun geldiginiz noktada onboarding, offboarding, rol, zimmet ve talep sureclerini tek platformdan yonetebiliyoruz. Son donemde AI katmaninda da guvenli bir model kurduk: sadece onayli ve salt-okunur SQL calisiyor; boylece hiz kazanirken risk kontrolunu de kaybetmiyoruz. Bundan sonraki odagimiz KPI bazli olcumleri canli verilerle takip edip yayginlastirmayi hizlandirmak. Bu platformu teknik bir arac olarak degil, operasyonel verimlilik ve uyum yatirimi olarak konumluyoruz."

---

## Ek - PDF Icın Tasarim Onerisi

- PDF alirken A4 dikey, ust-alt kenar bosluklar standart (2 cm) secilsin
- Basliklarin tek font hiyerarsisi korunmali (`H1 > H2 > H3`)
- Tablolarin satir bolmeleri acik olsun
- Sunumda en fazla 1 sayfada 1 ana mesaj ilkesine uyulsun
