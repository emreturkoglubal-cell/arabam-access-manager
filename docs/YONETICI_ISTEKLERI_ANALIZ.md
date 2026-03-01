# Account Manager — Yönetici İstekleri Analiz Raporu

Bu doküman, **arabam-access-manager** projesinin mevcut yapısı incelendikten sonra, yöneticiden gelen geliştirme taleplerinin **yapılabilirliği**, **nasıl yapılabileceği** ve **önerilen öncelik** açısından analizini içerir.

---

## 1. Proje Özeti (Mevcut Durum)

- **Teknoloji:** ASP.NET Core MVC, PostgreSQL, Dapper, QuestPDF, Chart.js, Bootstrap.
- **Sayfalar:** Personel (CRUD, detay, yetkiler, zimmet), Departmanlar, Uygulamalar (Systems), Donanım & Zimmet, İşe Giriş (Onboarding), İşten Çıkış (Offboarding), Raporlar, Ana sayfa (dashboard).
- **Önemli entity’ler:** `Personnel`, `Department`, `Asset`, `AssetAssignment`, `ResourceSystem`, `PersonnelAccess`, `Manager` (hiyerarşi).
- **Zimmet:** `ZimmetPdfService` ile tek zimmet için PDF üretiliyor; personel detayda sadece **aktif** zimmetler listeleniyor; iade edilen zimmetlerde **teslim alan** (returned_by) bilgisi tutulmuyor.

---

## 2. Personel Detay Sayfası

### 2.1 Zimmet formu – “PDF oluştur” ile tüm verilerin otomatik doldurulması

**İstek:** Gönderilen zimmet formundaki donanım bölümünde “PDF oluştur” butonu; tıklanınca tüm veriler otomatik doldurulsun.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Personel detayda “Zimmetteki donanım” listesi var; her zimmet için ayrı ayrı PDF şu an sadece **Donanım detay** sayfasından (Assets/Detail) üretiliyor. Personel detayda zimmet listesine **“PDF oluştur”** butonu eklenebilir; bu buton mevcut `ZimmetPdfService` ve `Assets/ZimmetPdf?assignmentId=` endpoint’i kullanılarak tek zimmet PDF’i indirilir.  
“Tüm veriler otomatik doldurulsun” ifadesi: Ya **tek zimmet için** mevcut PDF’in indirilmesi (zaten dolu) ya da **tüm zimmetleri tek PDF’de** toplayan yeni bir “toplu zimmet belgesi” üretmek olarak yorumlanabilir.

**Nasıl yapılır:**
- Personel detay view’da her zimmet satırına “PDF oluştur” linki: `Assets/ZimmetPdf?assignmentId=@z.Id` (yeni pencerede).
- İstenirse: Tüm zimmetler için tek PDF (çok sayfalı) üreten yeni bir action + `ZimmetPdfService` metodu (örn. `GenerateMultiAssignmentPdf`).

**Öncelik / Effort:** Düşük (tek zimmet linki) / Orta (toplu PDF).

---

### 2.2 Zimmet Onay bölümü – Teslim eden / Teslim alan

**İstek:** Zimmeti yapan kişi = Teslim eden; işe giren personel = Teslim alan. Formda bu alanlar otomatik doldurulsun.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** PDF’de zaten “Zimmetleyen” (AssignedByUserName) ve “Zimmette (kişi)” (personel adı) var. İstekteki “Teslim eden” = zimmeti yapan (AssignedBy), “Teslim alan” = zimmete alan personel; bu mantık mevcut PDF ile uyumlu. Sadece etiketleri “Teslim eden” / “Teslim alan” olarak netleştirmek yeterli olabilir.

**Nasıl yapılır:** `ZimmetPdfService.GeneratePdf` içinde metinleri “Teslim eden” / “Teslim alan” olarak güncellemek; view’da (eğer form görünümü varsa) aynı terimleri kullanmak.

**Öncelik / Effort:** Düşük.

---

### 2.3 İşten ayrılsa bile zimmet kaydı kalsın (geçmişe dönük)

**İstek:** Kişi işten ayrılsa bile zimmet kayıtları personel detayda görünsün (geçmişe dönük).

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Personel detayda sadece `GetActiveAssignmentsByPersonnel` kullanılıyor; yani **iade edilmemiş** zimmetler gösteriliyor. İşten ayrılan kişinin zimmetleri iade edildikten sonra listeden tamamen kayboluyor. Veritabanında tüm atamalar `asset_assignments` tablosunda duruyor; silinmiyor.

**Nasıl yapılır:**
- Personel detayda hem **aktif** hem **iade edilmiş** atamaları getiren bir metod kullanmak (örn. `GetByPersonnelId` zaten tümünü döndürüyor).
- View’da iki blok: “Aktif zimmetler” ve “Geçmiş zimmetler (iade edilenler)”. Geçmişte iade tarihi ve istenirse “teslim alan” (aşağıda) gösterilir.

**Öncelik / Effort:** Düşük–Orta (repo/service zaten var, view ve controller tarafında genişletme).

---

### 2.4 Zimmet İade bölümü – Teslim alan / Teslim eden

**İstek:** İade formunda: Teslim alan = işten çıkış işlemini yapan personel; Teslim eden = zimmeti iade eden (yani o personel kartındaki kişi).

**Yapılabilirlik:** ✅ **Yapılabilir** (veritabanı değişikliği gerekir).

**Mevcut durum:** `asset_assignments` tablosunda `returned_at`, `return_condition`, `notes` var; **iadeyi alan kişi** (returned_by_user_id / name) yok. Return işlemi `AssetService.Return` ile yapılıyor; kimin aldığı kaydedilmiyor.

**Nasıl yapılır:**
- `asset_assignments` tablosuna `returned_by_user_id`, `returned_by_user_name` (veya sadece name) eklenir.
- `AssetAssignment` entity ve repository (`SetReturned`) güncellenir.
- Return ekranında (Assets/Return) iadeyi yapan kullanıcı `_currentUser` ile alınır; Return çağrısına bu bilgi verilir.
- İade PDF’i veya zimmet detayında “İade alan: …” gösterilir.

**Öncelik / Effort:** Orta (migration + entity + service + UI).

---

### 2.5 Zimmetlenen donanım – Tür, Marka Model, Seri no, Zimmet tarihi

**İstek:** Listede/görünümde bu alanlar açık olsun.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Personel detayda zimmet listesinde asset adı ve tür (AssetType) var; marka/model ve seri no Asset’ten geliyor ama view’da gösterilmiyor. Zimmet tarihi (AssignedAt) gösteriliyor.

**Nasıl yapılır:** View’da her zimmet satırına Marka/Model, Seri no eklenir; AssetNames/AssetTypes gibi Asset bilgisi zaten dolduruluyor, BrandModel ve SerialNumber da ViewBag veya model ile eklenip gösterilir.

**Öncelik / Effort:** Düşük.

---

### 2.6 Zimmet bilgisi – Zimmeti yapan / iadeyi alan; eski zimmet pasif görünsün

**İstek:** Zimmeti yapan kişi ve işten ayrılışta zimmeti teslim alan kişi görünsün; kişi ayrıldığında zimmet başkasına verilse bile eski zimmet pasif olarak görünsün.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Zimmeti yapan (AssignedByUserName) zaten var. İadeyi alan için 2.4’teki kolonlar eklenecek. “Eski zimmet pasif görünsün”: Personel detayda geçmiş zimmetleri ayrı blokta göstermek (2.3) ile sağlanır; “pasif” = iade edilmiş, tarih ve “teslim alan” ile listelenir.

**Nasıl yapılır:** 2.3 + 2.4 uygulandığında bu madde de karşılanır.

**Öncelik / Effort:** 2.3 ve 2.4’e bağlı.

---

### 2.7 Kişisel bilgiler – Rol ve pozisyon kalkacak; Alt ekip ve ünvan gelecek

**İstek:** Kişisel bilgilerde Rol ve Pozisyon kaldırılsın; yerine “Alt ekip” ve “Ünvan” gelsin.

**Yapılabilirlik:** ✅ **Yapılabilir** (şema/terim netleştirmesi gerekir).

**Mevcut durum:** Personel entity’de `Position` ve `RoleId` var; detay ve düzenleme formunda ikisi de gösteriliyor. “Alt ekip” ve “Ünvan” için:  
- **Ünvan:** Genelde “Pozisyon” ile aynı anlama gelebilir; isim değişikliği (Pozisyon → Ünvan) veya ayrı bir alan (örn. `Title`) eklenebilir.  
- **Alt ekip:** Yeni bir kavram; departman altında ekibi temsil ediyor (örn. “DevOps”, “Backend”). Bu ya `Personnel`’a yeni alan (örn. `TeamId` veya `SubTeamName`) ya da departman altında “ekip” listesi (yeni tablo) ile modellenir.

**Nasıl yapılır:**
- Rol/Pozisyon: View ve formlardan kaldırılır; istenirse Rol backend’de tutulmaya devam eder (yetki ataması için).
- Ünvan: Label “Pozisyon” → “Ünvan” yapılır veya ayrı `Title` kolonu eklenir.
- Alt ekip: `departments` altında ekipler (yeni tablo `teams`: department_id, name) + `personnel.team_id` gibi bir FK; veya serbest metin alanı. İşe girişte “ekip” seçimi (2.8) ile tutarlı olmalı.

**Öncelik / Effort:** Orta (özellikle alt ekip için veri modeli kararı ve migration).

---

### 2.8 Schedule – Tarih + açıklama, mail ile uyarı

**İstek:** Tarih seçip açıklama yazıp “ne zaman bizi uyarmasını istiyoruz” belirlensin; o tarihte girilen açıklama mail atılsın.

**Yapılabilirlik:** ✅ **Yapılabilir** (yeni özellik, altyapı gerekir).

**Mevcut durum:** Projede personel bazlı “hatırlatma / schedule” ve mail gönderimi yok.

**Nasıl yapılır:**
- Yeni tablo: örn. `personnel_reminders` (personnel_id, reminder_date, description, created_at, sent_at, created_by).
- Personel detayda “Hatırlatma ekle” formu: tarih + açıklama; kayıt bu tabloya yazılır.
- Zamanlanmış job (Hangfire, IHostedService veya dış cron): Günlük çalışıp `reminder_date = bugün` olan kayıtlar için mail gönderir; `sent_at` güncellenir.
- Mail: SMTP veya SendGrid vb. (projede mail altyapısı yoksa eklenir).

**Öncelik / Effort:** Orta–Yüksek (yeni tablo, job, mail konfigürasyonu, güvenlik).

---

## 3. Departmanlar Sayfası

### 3.1 Açıklama yerine “Departman yöneticisi” (en üst yönetici – GMY/Direktör)

**İstek:** Departmanlar listesinde açıklama yerine, o departmana bağlı en üst yönetici (GMY/Direktör) ismi görünsün.

**Yapılabilirlik:** ✅ **Yapılabilir** (mantık tanımı gerekir).

**Mevcut durum:** Departmanlar listesinde Kod, Ad, Açıklama, Personel sayısı var. “En üst yönetici” için: Departman–yönetici ilişkisi doğrudan yok; personeller `manager_id` ile birbirine bağlı, `managers` tablosunda level 1 = en üst. “Departmanın en üst yöneticisi” = o departmandaki personellerden biri, kendi seviyesi 1 olan veya o departmanda en çok astı olan üst yönetici gibi bir kural gerekir. Alternatif: Departmana açıkça “yönetici” atanır (yeni alan `department_id` → `manager_personnel_id` veya mevcut bir tabloya bağlanır).

**Nasıl yapılır:**
- Seçenek A: `departments` tablosuna `top_manager_personnel_id` (veya benzeri) eklenir; departman düzenlerken “Departman yöneticisi” seçilir; listede bu isim gösterilir.
- Seçenek B: Kurala dayalı hesaplama (örn. bu departmandaki personellerin manager zincirinde level 1 olan); daha karmaşık ve performanslı sorgu gerekir.

**Öncelik / Effort:** Orta (A seçeneği daha basit ve net).

---

### 3.2 Departman bazlı personel sayısı grafiği; aylık/yıllık; anasayfada da

**İstek:** Departmanlar sayfasında ve anasayfada, departman bazlı personel sayısı grafiği; aylık ve yıllık değişim.

**Yapılabilirlik:** ✅ **Kısmen / Koşullu.**

**Mevcut durum:** Anasayfada “Departmanlara göre” grafiği var (`chartByDepartment`); muhtemelen o anki (anlık) personel sayıları. “Aylık/yıllık değişim” için **zaman içinde** departman bazlı sayı tutulması gerekir; şu an personel giriş/çıkış tarihleri var ama “X ayı sonundaki departman sayıları” için her ay için hesaplama yapılabilir (start_date / end_date ile). Doğru trend için ideal olan: periyodik snapshot (örn. aylık departman_personnel_count tablosu) ama mevcut veriyle de yaklaşık grafik üretilebilir.

**Nasıl yapılır:**
- Mevcut grafik: Anlık sayılar zaten var; “Departmanlar” sayfasına aynı grafik bileşeni eklenir.
- Aylık/yıllık değişim: `ReportService` (veya benzeri) içinde, seçilen ay/yl için o tarihteki aktif personel sayısını `StartDate <= ay_sonu AND (EndDate IS NULL OR EndDate > ay_sonu)` ile hesaplayan metod yazılır; bu veriyle çizgi/bar grafik doldurulur. İstenirse ileride aylık snapshot tablosu eklenip job ile doldurulur.

**Öncelik / Effort:** Orta (mevcut veriyle hesaplama) / Yüksek (snapshot + job).

---

### 3.3 Departmanlar bölümünde alt kırılımlar (örn. Bilgi Teknolojileri → DevOps, Development)

**İstek:** Ana departman altında alt departmanlar/ekipler (alt kırılım) olsun.

**Yapılabilirlik:** ✅ **Yapılabilir** (veritabanı değişikliği).

**Mevcut durum:** `departments` düz liste; `parent_id` yok.

**Nasıl yapılır:**
- `departments` tablosuna `parent_id INT REFERENCES departments(id)` eklenir.
- Department entity ve CRUD güncellenir; liste ağaç veya “ana / alt” gruplu gösterilir.
- Personel hâlâ tek bir `department_id` ile bağlı kalabilir (en alt seviye departman/ekip); veya hem departman hem ekip (2.7’deki “Alt ekip”) ile tutulabilir. Bu durumda “alt kırılım” departman ağacı mı yoksa ayrı “teams” mı olacak netleştirilmeli.

**Öncelik / Effort:** Orta–Yüksek (migration, UI ağaç/grup, filtreleme mantığı).

---

## 4. Departman Detay Sayfası

### 4.1 1. / 2. / 3. Yönetici alanları; birden fazla kişi; ünvan personel kartından

**İstek:** Departman detayda 1. Yönetici, 2. Yönetici, 3. Yönetici alanları; birden fazla kişi eklenebilsin (uygulama detayındaki sorumlu gibi); ünvan personel kartından gelsin.

**Yapılabilirlik:** ✅ **Yapılabilir** (yeni tablo/ilişki).

**Mevcut durum:** Uygulama (ResourceSystem) tarafında `resource_system_owners` ile N-N sorumlu atanıyor. Departmanda böyle bir yapı yok.

**Nasıl yapılır:**
- Yeni tablo: örn. `department_managers` (department_id, personnel_id, level 1/2/3, order). Bir departmanda aynı level’da birden fazla kişi olabilir.
- Departman detay/düzenleme sayfasında level 1/2/3 için çoklu personel seçimi (Systems/Edit’teki owner seçimine benzer); listelemede personel adı + `Personnel.Position` (veya Ünvan) gösterilir.

**Öncelik / Effort:** Orta.

---

### 4.2 Grafik: Aylık giren/çıkan, turnover, ekip toplam lisans maliyeti (USD)

**İstek:** Departman detayda aylık giren/çıkan personel, turnover, departmandaki toplam lisans maliyeti (USD).

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Personel start_date/end_date var; bu departmandaki personellerin aylık giriş/çıkış sayıları hesaplanabilir. Turnover = çıkan / (ortalama veya dönem sonu aktif) gibi formül. Lisans maliyeti: Bu departmandaki personellerin aktif `personnel_accesses` + `resource_systems.unit_cost` ile hesaplanıyor (personel detayda ApplicationCostUsd benzeri); departman bazlı toplam için aynı mantık kullanılır.

**Nasıl yapılır:**
- ReportService’e (veya DepartmentService) departman id + ay aralığı için: giren sayısı, çıkan sayısı, turnover, toplam lisans maliyeti USD döndüren metodlar eklenir.
- Departman detay view’da grafik (Chart.js) ve özet kutusu eklenir.

**Öncelik / Effort:** Orta.

---

## 5. Uygulamalar Sayfası

### 5.1 Uygulamalar listesinde grafik – aylık kullanıma bağlı fiyat değişiklikleri (özet rapor)

**İstek:** Uygulamalar listesinde, uygulamaların aylık kullanıma bağlı fiyat değişiklikleri grafikle gösterilsin; hangi uygulamada ne kadar artış var görülsün.

**Yapılabilirlik:** ⚠️ **Kısmen** (tarihsel veri yoksa tahmini).

**Mevcut durum:** `resource_systems` içinde `unit_cost` ve para birimi var; aktif erişim sayısı anlık. “Aylık kullanıma bağlı fiyat değişiklikleri” için geçmiş aylarda o uygulamadaki erişim sayısı ve/veya unit_cost değişimi gerekir. Erişim sayısı geçmişi tutulmuyor (personnel_accesses’te granted_at var, revoke edilenler için bitiş yok). Unit_cost geçmişi de yok.

**Nasıl yapılır:**
- Kısa vadede: Sadece “şu anki” maliyet (unit_cost × aktif erişim) listelenir; grafik “uygulamalar × toplam maliyet” bar grafiği olur; “değişim” olmaz.
- Tam çözüm: Aylık snapshot tablosu (uygulama_id, ay, erişim_sayısı, toplam_maliyet) bir job ile doldurulur; son 6–12 ay verisiyle artış/azalış grafiği çizilir.

**Öncelik / Effort:** Basit versiyon düşük; tam versiyon orta–yüksek.

---

## 6. Uygulama Detay Sayfası

### 6.1 Aylık grafik/sayısal artış–düşüş; son 6 ay; artış kırmızı, düşüş yeşil; departman bazlı kullanım ve maliyet

**İstek:** Uygulama detayda aylık artış/düşüş (son 6 ay), renkli; departman bazlı kullanım adetleri ve maliyetleri.

**Yapılabilirlik:** ⚠️ **Geçmiş veri ile kısmen.**

**Mevcut durum:** Anlık erişim sayısı ve departman dağılımı (bu sistemde yetkisi olan personellerin departmanları) hesaplanabilir. Geçmiş aylardaki erişim sayısı için ya personnel_accesses geçmişi (granted_at / revoked) ya da aylık snapshot gerekir.

**Nasıl yapılır:**
- Şu anki durum: Bu uygulamadaki aktif erişim sayısı + departman kırılımı (personel → department) ve departman bazlı maliyet gösterilir; “son 6 ay trend” olmaz.
- Trend için: Aylık snapshot veya access history tablosu gerekir (5.1 ile aynı konu).

**Öncelik / Effort:** Anlık + departman kırılımı orta; trend yüksek.

---

## 7. Donanım ve Zimmet Sayfası

### 7.1 “Ad” yerine “Barkod”

**İstek:** Ad sütununun adı “Barkod” olarak değişsin.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Nasıl yapılır:** Assets Index view’da tablo başlığı “Ad” → “Barkod” yapılır. (İçerik hâlâ `asset.Name` veya barkod için ayrı alan kullanılıyorsa o alan gösterilir; sadece etiket değişikliği ise view değişikliği yeterli.)

**Öncelik / Effort:** Düşük.

---

### 7.2 Amortisman bitiş tarihi

**İstek:** Amortisman bitiş tarihi eklensin.

**Yapılabilirlik:** ✅ **Zaten var.**

**Mevcut durum:** `assets` tablosunda `depreciation_end_date` (migration 07) var; Create/Edit ve Detail’da kullanılıyor.

**Nasıl yapılır:** Listede (Index) bu kolonu göstermek istenirse, Assets Index’e “Amortisman bitiş” sütunu eklenir.

**Öncelik / Effort:** Düşük.

---

### 7.3 Arama kutusu (kişi, marka, model vb.)

**İstek:** Search box; kişi, marka, model vb. ile arama.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Assets Index’te sadece durum ve tür filtresi var; metin araması yok.

**Nasıl yapılır:** `Assets/Index`’e `search` parametresi eklenir; repository’de name, serial_number, brand_model ve zimmetteki personel adına (JOIN asset_assignments + personnel) göre LIKE/ILIKE filtresi uygulanır.

**Öncelik / Effort:** Orta.

---

### 7.4 En alta grafik (toplam envanter, amortisman süresi dolacak, kullanımda, depoda, hurda, satılacak, test)

**İstek:** Donanım sayfasında toplam envanter, amortismanı dolacak, kullanımda, depoda, hurda, satılacak, test gibi kırılımlarda grafik.

**Yapılabilirlik:** ⚠️ **Kısmen.** (Bazı durumlar yok.)

**Mevcut durum:** `AssetStatus`: Available, Assigned, InRepair, Retired. “Depoda”, “satılacak”, “test” gibi ayrı durumlar yok. Amortisman bitiş tarihi var; “süresi dolacak” (örn. 30 gün içinde) hesaplanabilir.

**Nasıl yapılır:**
- Mevcut 4 durum + “Amortismanı yakında bitecek” (depreciation_end_date BETWEEN bugün AND bugün+30) için pasta/bar grafik eklenir.
- “Depoda / satılacak / test” için ya AssetStatus enum’a yeni değerler eklenir (migration + kod) ya da “Not”/etiket alanı ile ayrıştırılır; yöneticiyle netleştirmek iyi olur.

**Öncelik / Effort:** Mevcut durumlarla düşük–orta; yeni durumlarla orta.

---

## 8. Donanım Detay Sayfası

### 8.1 Cihaz türüne göre spesifik özellikler (Laptop: RAM, HDD, İşlemci, Ekran; Telefon/Tablet: Ekran, RAM, Hafıza; Monitör: Pivot, Ekran boyutu)

**İstek:** Dizüstü, telefon, tablet, monitör için farklı özellik alanları.

**Yapılabilirlik:** ✅ **Yapılabilir** (şema + UI).

**Mevcut durum:** Asset’te tür (AssetType) var; ek özellik alanları yok.

**Nasıl yapılır:**
- Seçenek A: `assets` tablosuna nullable kolonlar eklenir: ram_gb, storage_gb, cpu, screen_inches, is_pivot (monitör) vb.; form ve detay view türe göre bu alanları gösterir.
- Seçenek B: JSONB bir “specs” kolonu; esnek ama raporlama zor.  
Öneri: A; türe göre sadece ilgili alanlar doldurulur/gösterilir.

**Öncelik / Effort:** Orta (migration + form/detay koşullu alanlar).

---

### 8.2 Amortisman süresi (yıl); amortisman bitiş tarihi; toplam maliyet / aylık / kalan

**İstek:** Amortisman süresi yıl olarak (1–5); amortisman bitiş tarihi; toplam maliyet, amortisman süresine göre aylık ve kalan maliyet.

**Yapılabilirlik:** ✅ **Büyük ölçüde yapılabilir.**

**Mevcut durum:** `depreciation_end_date` ve `purchase_date`, `purchase_price` var. “Amortisman süresi (yıl)” şu an sadece bitiş tarihinden çıkarılıyor; açık “amortisman_yil” alanı yok. Aylık = toplam / (süre ay); kalan = (bitişe kalan ay) × aylık veya doğrusal amortisman formülü.

**Nasıl yapılır:**
- `assets`’e `depreciation_years` (1–5) eklenebilir; Create/Edit’te seçilir; bitiş tarihi satın alma + depreciation_years ile de set edilebilir.
- Detay sayfasında: Toplam maliyet, amortisman süresi (ay), aylık maliyet, bugüne kadar geçen süre, kalan maliyet hesaplanıp gösterilir.

**Öncelik / Effort:** Orta.

---

### 8.3 Zimmet bilgisinde yönetici adı ve zimmetleyen adı

**İstek:** Zimmet bilgisi kısmında yöneticinin ve zimmetleyenin adı görünsün.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Donanım detayda zimmette “Zimmetleyen” (AssignedByUserName) var; personelin yöneticisi (Manager) ayrı sorgulanıp gösterilebilir.

**Nasıl yapılır:** Detail action’da zimmetteki personelin ManagerId’si ile Personnel getirilir; view’da “Yönetici: …” ve “Zimmetleyen: …” yazdırılır.

**Öncelik / Effort:** Düşük.

---

## 9. İşe Giriş

### 9.1 İşe girenler listesi (Son 1 ay, filtre)

**İstek:** İşe giriş işleminden sonra, işe girenler altta listelensin; “Son 1 ay” ve filtre.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Onboarding sadece form; işe girenleri listeleyen ayrı bir blok yok. Personel listesi zaten StartDate ile filtrelenebilir.

**Nasıl yapılır:** Onboarding/Index sayfasının altına “Son X ay içinde işe girenler” listesi eklenir; PersonnelService’e StartDate >= (bugün - X ay) filtresi ile liste alınır; sayfa parametresi (örn. lastMonths=1) ve isteğe bağlı departman/arama filtresi eklenir.

**Öncelik / Effort:** Düşük–Orta.

---

### 9.2 Departman yanına “Ekip”; departmana göre ekipler

**İstek:** İşe girişte departman seçilince, o departmana ait ekipler listelensin; ekip seçilsin.

**Yapılabilirlik:** ✅ **Yapılabilir** (2.7 / 3.3 ile uyumlu).

**Mevcut durum:** Sadece departman seçimi var. Ekip ya departman alt kırılımı (parent_id) ya da ayrı `teams` tablosu (department_id) ile modellenir.

**Nasıl yapılır:** 2.7’deki “Alt ekip” ve 3.3’teki departman alt yapı ile birlikte; Onboarding formuna Ekip dropdown’ı eklenir; departman seçilince ekipler AJAX veya sayfa yenileme ile doldurulur. Personnel’a team_id (veya equivalent) eklenir.

**Öncelik / Effort:** Orta (ekip modeline bağlı).

---

### 9.3 Seviye (Jr, Mid, Sr, Lead vb.)

**İstek:** Seviye alanı eklensin.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Personnel’da böyle bir alan yok.

**Nasıl yapılır:** `personnel` tablosuna `level` veya `seniority` (VARCHAR veya SMALLINT/enum); değerler: Jr, Mid, Sr, Lead vb. Form ve listelerde gösterilir.

**Öncelik / Effort:** Düşük–Orta.

---

### 9.4 Pozisyon adı “Ünvan” olsun; girilen bilgilere göre otomatik doldurulsun

**İstek:** Pozisyon alanı “Ünvan” olarak adlandırılsın; girilen bilgilere göre otomatik doldurulsun.

**Yapılabilirlik:** ✅ **Kısmen (otomatik doldurma belirsiz).**

**Mevcut durum:** Position (Pozisyon) manuel metin.

**Nasıl yapılır:** Label “Ünvan” yapılır. “Otomatik doldurulsun”: Departman + Ekip + Seviye seçimine göre bir “şablon ünvan” (örn. “Backend Developer - Sr”) oluşturulabilir; bu bir kural/şablon tablosu veya basit bir birleştirme (Departman adı + Seviye) olabilir. Tam otomatik için iş kuralı netleştirilmeli.

**Öncelik / Effort:** Etiket düşük; otomatik şablon orta.

---

## 10. İşten Çıkış

### 10.1 İşten çıkanlar listesi (Son 1 ay, filtre)

**İstek:** İşten çıkış işleminden sonra, işten çıkanlar altta listelensin; Son 1 ay ve filtre.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Offboarding’de sadece form ve Result sayfası var; Raporlar’da “İşten çıkanlar” tablosu var (tarih aralığı ile). Ayrıca “Son 1 ay” filtresi yok.

**Nasıl yapılır:** Offboarding/Index sayfasının altına (veya ayrı bir blokta) “Son X ay içinde işten çıkanlar” listesi eklenir; `GetOffboardedReport(from, to)` benzeri bir çağrı ile EndDate son X ay içinde olanlar getirilir; filtre (tarih, departman) eklenir.

**Öncelik / Effort:** Düşük–Orta.

---

### 10.2 İşten çıkanlar listesinde PDF (zimmet formu) oluşturma

**İstek:** İşten çıkanlar listesinde her kişi için zimmet formu PDF’i oluşturulabilsin.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** Zimmet PDF tek atama için var; işten çıkan kişinin tüm (aktif veya son) zimmetleri için toplu PDF veya her zimmet için link verilebilir.

**Nasıl yapılır:** İşten çıkanlar listesinde her satırda “Zimmet PDF” linki; o personelin (iade edilmemiş veya son) zimmetleri için ya tek tek PDF linkleri ya da “Tüm zimmetler tek PDF” action’ı. İşten çıkış anında zimmetler genelde iade edilmiş olacağı için “iade edilen son zimmetler” için de iade belgesi/PDF istenebilir; o zaman 2.4’teki “teslim alan” ile birlikte iade PDF’i tasarlanır.

**Öncelik / Effort:** Orta (toplu PDF varsa).

---

### 10.3 Listede: Ad soyad, işe giriş, işten çıkış, mail; yeşil/kırmızı (açık hesap)

**İstek:** İşten çıkanlar listesinde ad soyad, işe giriş, işten çıkış, mail; açık hesabı kalanlar kırmızı, diğerleri yeşil.

**Yapılabilirlik:** ✅ **Yapılabilir.**

**Mevcut durum:** OffboardedReportRow’da PersonnelId, FullName, EndDate, Department var; Email ve StartDate eklenebilir. “Açık hesap” = o personelin hâlâ aktif (is_active) personnel_accesses kaydı var mı; varsa kırmızı, yoksa yeşil.

**Nasıl yapılır:** OffboardedReportRow’a Email, StartDate eklenir; rapor çekerken bu personellerin aktif erişim sayısı da döner (veya ayrı sorgu); view’da satır rengi (class) buna göre yeşil/kırmızı yapılır.

**Öncelik / Effort:** Düşük–Orta.

---

## 11. Özet Tablo ve Önerilen Sıra

| # | Konu | Yapılabilir? | Effort | Veritabanı | Öncelik önerisi |
|---|------|--------------|--------|------------|------------------|
| 2.1 | Zimmet PDF – personel detayda buton / toplu PDF | ✅ | Düşük–Orta | Hayır | Yüksek |
| 2.2 | Teslim eden / Teslim alan etiketleri | ✅ | Düşük | Hayır | Yüksek |
| 2.3 | İşten ayrılsa bile zimmet geçmişi kalsın | ✅ | Düşük–Orta | Hayır | Yüksek |
| 2.4 | Zimmet iade – teslim alan (returned_by) | ✅ | Orta | Evet | Yüksek |
| 2.5 | Zimmet listesinde tür, marka/model, seri no, tarih | ✅ | Düşük | Hayır | Orta |
| 2.6 | Zimmette zimmeti yapan + iadeyi alan; eski pasif | ✅ | 2.3+2.4 | Evet (2.4) | 2.3–2.4 ile |
| 2.7 | Rol/Pozisyon kalkacak; Alt ekip + Ünvan | ✅ | Orta | Evet (ekip/ünvan) | Orta |
| 2.8 | Schedule + mail uyarı | ✅ | Orta–Yüksek | Evet | Orta |
| 3.1 | Departman listesinde GMY/Direktör ismi | ✅ | Orta | Opsiyonel | Orta |
| 3.2 | Departman grafiği; aylık/yıllık; anasayfa | ✅ Kısmen | Orta | Opsiyonel snapshot | Orta |
| 3.3 | Departman alt kırılımları | ✅ | Orta–Yüksek | Evet | Orta |
| 4.1 | Departman 1./2./3. yönetici; çoklu; ünvan | ✅ | Orta | Evet | Orta |
| 4.2 | Departman detay grafik (giren/çıkan, turnover, maliyet) | ✅ | Orta | Hayır | Orta |
| 5.1 | Uygulamalar – aylık maliyet değişim grafiği | ⚠️ Kısmen | Orta–Yüksek | Snapshot ise evet | Düşük |
| 6.1 | Uygulama detay – trend + departman maliyet | ⚠️ Kısmen | Orta–Yüksek | Snapshot ise evet | Düşük |
| 7.1 | Donanım listesi “Barkod” etiketi | ✅ | Düşük | Hayır | Düşük |
| 7.2 | Amortisman bitiş tarihi (listede) | ✅ Var | Düşük | Hayır | Düşük |
| 7.3 | Donanım arama (kişi, marka, model) | ✅ | Orta | Hayır | Yüksek |
| 7.4 | Donanım grafik (envanter, durumlar) | ⚠️ Kısmen | Orta | Opsiyonel | Orta |
| 8.1 | Cihaz türüne göre özellikler | ✅ | Orta | Evet | Orta |
| 8.2 | Amortisman süresi (yıl), aylık/kalan maliyet | ✅ | Orta | Opsiyonel | Orta |
| 8.3 | Zimmet bilgisinde yönetici + zimmetleyen | ✅ | Düşük | Hayır | Orta |
| 9.1 | İşe girenler listesi (Son 1 ay, filtre) | ✅ | Düşük–Orta | Hayır | Yüksek |
| 9.2 | İşe girişte ekip (departmana göre) | ✅ | Orta | Evet (ekip) | Orta |
| 9.3 | Seviye (Jr, Mid, Sr, Lead) | ✅ | Düşük–Orta | Evet | Orta |
| 9.4 | Ünvan; otomatik doldurma | ✅ Kısmen | Orta | Hayır/Opsiyonel | Orta |
| 10.1 | İşten çıkanlar listesi (Son 1 ay, filtre) | ✅ | Düşük–Orta | Hayır | Yüksek |
| 10.2 | İşten çıkanlar – Zimmet PDF | ✅ | Orta | Hayır | Yüksek |
| 10.3 | Listede ad/soyad, giriş/çıkış, mail; yeşil/kırmızı | ✅ | Düşük–Orta | Hayır | Yüksek |

---

## 12. Genel Öneriler

1. **Önce zimmet ve işten çıkış akışı:** 2.1–2.6 ve 10.1–10.3, kullanıcıların günlük ihtiyacı için öncelikli; 2.4 (returned_by) diğer iade ekranları ve raporlarla tutarlılık için önemli.
2. **İşe giriş/çıkış listeleri:** 9.1 ve 10.1 hızlı kazanım; mevcut personel ve rapor servisleriyle yapılabilir.
3. **Departman hiyerarşisi ve ekip:** 3.3 (alt kırılım) ile 2.7 (alt ekip) ve 9.2 (ekip seçimi) birlikte tasarlanırsa tek migration ve tek terminoloji kullanılır.
4. **Grafik/trend (uygulama maliyeti, aylık değişim):** Geçmiş veri olmadan sadece “anlık” gösterim yapılır; aylık snapshot tablosu ve job ile tam çözüm planlanabilir.
5. **Schedule + mail:** Mail altyapısı (SMTP/SendGrid) ve zamanlanmış job gerekir; güvenlik ve izinler netleştirilmeli.

Bu analiz, mevcut kod ve veritabanına göre hazırlanmıştır; iş kuralları (örn. “en üst yönetici” tanımı, “otomatik ünvan”) netleştirildikçe uygulama detayları güncellenebilir.
