# Canlıya Çıkış Öncesi Kontrol Listesi — Yönetici İstekleri

Bu doküman, **YONETICI_ISTEKLERI_ANALIZ.md** maddelerine göre yapılan geliştirmelerin durumunu, veritabanı migration’ının doğruluğunu ve proje tasarımına uyumu özetler.

---

## 1. İstek Bazlı Durum Özeti

| # | İstek | Durum | Not |
|---|--------|--------|-----|
| **2.1** | Zimmet PDF – personel detayda buton | ✅ Yapıldı | Her zimmet satırında "PDF oluştur" → `Assets/ZimmetPdf?assignmentId=` |
| **2.2** | Teslim eden / Teslim alan etiketleri | ✅ Yapıldı | ZimmetPdfService ve view metinleri güncellendi |
| **2.3** | İşten ayrılsa bile zimmet geçmişi kalsın | ✅ Yapıldı | Personel detayda "Aktif zimmetler" + "Geçmiş zimmetler (iade edilen)" |
| **2.4** | Zimmet iade – teslim alan (returned_by) | ✅ Yapıldı | DB: returned_by_user_id, returned_by_user_name; Return ekranında kaydediliyor |
| **2.5** | Zimmet listesinde tür, marka/model, seri no, tarih | ✅ Yapıldı | Personel detay zimmet tablosunda gösteriliyor |
| **2.6** | Zimmette zimmeti yapan + iadeyi alan; eski pasif | ✅ Yapıldı | 2.3 + 2.4 ile karşılandı |
| **2.7** | Rol/Pozisyon kalkacak; Alt ekip + Ünvan | ✅ Yapıldı | Kişisel bilgilerde Team (Alt ekip), Position (Ünvan); düzenlemede TeamId, SeniorityLevel |
| **2.8** | Schedule + mail uyarı | ⚠️ Kısmen | Tablo, repo, service, personel detayda "Hatırlatma" formu var; **mail job / SMTP yok** (ileride eklenebilir) |
| **3.1** | Departman listesinde GMY/Direktör ismi | ✅ Yapıldı | "Departman Yöneticisi (GMY)" sütunu; düzenlemede atanıyor |
| **3.2** | Departman grafiği; aylık/yıllık; anasayfa | ❌ Yapılmadı | Snapshot/grafik UI eklenmedi |
| **3.3** | Departman alt kırılımları (parent_id) | ⚠️ DB hazır | parent_id migration’da; liste/detayda ağaç veya parent seçimi UI yok |
| **4.1** | Departman 1./2./3. yönetici; çoklu; ünvan | ⚠️ Kısmen | Tablo + listeleme (detayda kart) var; **yönetici ekleme/çıkarma formu/modal yok** |
| **4.2** | Departman detay grafik (giren/çıkan, turnover, maliyet) | ❌ Yapılmadı | |
| **5.1** | Uygulamalar – aylık maliyet değişim grafiği | ⚠️ Tablo hazır | resource_system_monthly_snapshots var; job + grafik UI yok |
| **6.1** | Uygulama detay – trend + departman maliyet | ❌ Yapılmadı | Geçmiş veri olmadan anlamlı değil |
| **7.1** | Donanım listesi "Barkod" etiketi | ✅ Yapıldı | "Ad" → "Barkod" |
| **7.2** | Amortisman bitiş tarihi (listede) | ✅ Yapıldı | Assets Index’te sütun + "Amortisman bitiş" |
| **7.3** | Donanım arama (kişi, marka, model) | ✅ Yapıldı | search parametresi; name, serial_number, brand_model, personel adı |
| **7.4** | Donanım grafik (envanter, durumlar) | ✅ Yapıldı | Envanter özeti (CountByStatus, DepreciationEndingSoon); ForSale/Test filtreleri |
| **8.1** | Cihaz türüne göre özellikler | ✅ Yapıldı | spec_ram_gb, spec_storage_gb, spec_cpu, spec_screen_inches, spec_is_pivot; Create/Edit/Detail |
| **8.2** | Amortisman süresi (yıl), aylık/kalan maliyet | ✅ Yapıldı | depreciation_years; detayda hesaplama ve gösterim |
| **8.3** | Zimmet bilgisinde yönetici + zimmetleyen | ✅ Yapıldı | Asset detayda "Yönetici" ve "Zimmetleyen (Teslim eden)" |
| **9.1** | İşe girenler listesi (Son 1 ay) | ✅ Yapıldı | Onboarding sayfasında "Son 1 ay içinde işe girenler" tablosu |
| **9.2** | İşe girişte ekip (departmana göre) | ✅ Yapıldı | Ekip dropdown; departman seçilince filtre (data-department-id) |
| **9.3** | Seviye (Jr, Mid, Sr, Lead) | ✅ Yapıldı | personnel.seniority_level; Onboarding ve Personel düzenlemede |
| **9.4** | Ünvan; otomatik doldurma | ✅ Kısmen | Label "Ünvan"; otomatik şablon yok |
| **10.1** | İşten çıkanlar listesi (Son 1 ay) | ✅ Yapıldı | Offboarding/Index’te "Son 1 ay içinde işten çıkanlar" |
| **10.2** | İşten çıkanlar – Zimmet PDF | ✅ Yapıldı | "Zimmet / PDF" → Personel detay (oradan tek tek PDF) |
| **10.3** | Listede ad/soyad, giriş/çıkış, mail; yeşil/kırmızı | ✅ Yapıldı | OffboardedReportRow + satır rengi (HasOpenAccess) |

**Özet:** Çoğu yüksek/orta öncelikli istek karşılandı. Bilinçli olarak yapılmayan veya kısmen bırakılanlar: grafikler (3.2, 4.2, 5.1, 6.1), departman yönetici atama UI (4.1), departman parent UI (3.3), schedule mail job (2.8).

---

## 2. Veritabanı Migration Kontrolü

**Dosya:** `database/15_yonetici_istekleri_migration.sql`

### 2.1 İsimlendirme ve Kurallar

- **Tablo/kolon:** Tüm yeni tablolar ve kolonlar **snake_case**; proje standardı (01_create_tables) ile uyumlu.
- **Index isimleri:** `ix_<tablo>_<kolon>` (örn. `ix_teams_department_id`, `ix_department_managers_department_id`). Mevcut migration’larla (01, 04, 10) tutarlı.
- **Unique constraint:** `uq_department_managers_dept_person_level`, `uq_snapshot_system_month` — açıklayıcı ve tutarlı.
- **Check constraint:** `chk_assets_status`, `chk_department_managers_level` — mevcut `chk_*` kullanımına uygun.

### 2.2 İçerik Doğrulama

| Bölüm | Tablo/Kolon | Kontrol |
|-------|-------------|--------|
| 15.1 | asset_assignments: returned_by_user_id, returned_by_user_name | ✅ ADD COLUMN IF NOT EXISTS; COMMENT var |
| 15.2 | personnel: seniority_level, team_id | ✅ |
| 15.3 | teams (id, department_id, name, code, created_at) | ✅ FK departments; ix_teams_department_id, ix_teams_name |
| 15.3 | personnel.team_id → teams(id) | ✅ DO block ile FK; table_schema = 'public' ile güvenli kontrol |
| 15.4 | departments: parent_id, top_manager_personnel_id | ✅ FK; ix_departments_parent_id, ix_departments_top_manager (WHERE NOT NULL) |
| 15.5 | department_managers (1/2/3. yönetici) | ✅ chk 1,2,3; uq (department_id, personnel_id, manager_level); ix’ler |
| 15.6 | personnel_reminders | ✅ created_by_user_id, created_by_user_name; ix’ler; sent_at WHERE sent_at IS NULL |
| 15.7 | assets: depreciation_years, spec_*, status 4,5 | ✅ chk_assets_status DROP + ADD (0..5) |
| 15.8 | resource_system_monthly_snapshots | ✅ uq (resource_system_id, year_month); ix’ler |

### 2.3 Sıra ve Bağımlılıklar

- `teams` 15.3’te oluşturuluyor; `personnel.team_id` FK’si aynı dosyada sonradan DO block ile ekleniyor — doğru.
- `departments.parent_id` self-FK; `top_manager_personnel_id` → personnel — tablolar mevcut.
- Diğer FK’ler (department_managers → departments, personnel; personnel_reminders → personnel; snapshots → resource_systems) hepsi mevcut tablolara referans — sıra uygun.

**Sonuç:** Migration script’i proje kurallarına ve PostgreSQL pratiklerine uygun; canlıda **01–14 sonrasında tek sefer** çalıştırılmalı.

---

## 3. Tasarım ve Proje Yapısına Uyum

### 3.1 Katmanlar

- **Domain:** Yeni entity’ler (Team, DepartmentManager, PersonnelReminder) ve güncellenenler (Asset, AssetAssignment, Department, Personnel) Domain’de; enum (AssetStatus) güncel.
- **Application:** Dto’lar (OffboardedReportRow), interface’ler (ITeamService, IPersonnelReminderService, IReportService.GetOffboardedReport, IAssetService.GetAssignmentsByPersonnel) Application katmanında.
- **Infrastructure:** Tüm repository ve service implementasyonları Infrastructure’da; Dapper + Npgsql, snake_case kolon eşlemesi.
- **Web (UI):** Controller’lar mevcut isimlendirme ve klasör yapısına uygun; View’lar am-card, am-table, btn-am-* sınıfları ile tutarlı.

### 3.2 Tutarlılık Kontrolleri

- **Repository:** SELECT/INSERT/UPDATE’lerde yeni kolonlar (returned_by, seniority_level, team_id, spec_*, depreciation_years, parent_id, top_manager_personnel_id) kullanılıyor.
- **Service:** AssetService.Return(..., returnedByUserId, returnedByUserName); DepartmentService GetDepartmentManagers/SetDepartmentManagers; PersonnelReminderService; TeamService — hepsi ilgili repo’larla uyumlu.
- **DI:** Yeni repo ve service’ler `ServiceCollectionExtensions` içinde kayıtlı.
- **View’lar:** ViewBag ve model kullanımı mevcut sayfalarla aynı pattern’de (örn. Personnel Detail, Departments Index/Detail, Assets Index/Detail, Onboarding, Offboarding).

### 3.3 Eksik veya İsteğe Bağlı Parçalar

- **Departman 1./2./3. yönetici atama:** `SetDepartmentManagers` var; UI’da sadece listeleme var, ekleme/silme formu yok. Canlıda manuel SQL veya sonraki sprint’te küçük bir modal ile eklenebilir.
- **Departman parent (alt kırılım):** parent_id DB’de var; liste/detay/create/edit’te ağaç veya parent seçimi yok.
- **Schedule mail:** Hatırlatma kaydı ve listesi var; belirli tarihte mail atacak job (Hangfire/IHostedService) ve SMTP/SendGrid konfigürasyonu yok.
- **Grafikler:** Anlık veri kullanılan yerler (örn. envanter özeti) yapıldı; aylık/yıllık trend grafikleri ve snapshot dolduran job yapılmadı.

Bu maddeler analiz dokümanında da “kısmen” veya “ileride” olarak geçiyor; canlıya çıkış için zorunlu değil.

---

## 4. Canlıya Çıkış Öncesi Yapılacaklar

1. **Migration:** Canlı veritabanında `01`–`14` uygulandıysa, `15_yonetici_istekleri_migration.sql` tek sefer çalıştırılmalı (bakım penceresinde tercih edilir).
2. **Build:** `dotnet build` hatasız ve mümkünse uyarısız (şu an 0 Error, 0 Warning).
3. **Config:** Connection string ve gerekli ayarlar canlı ortam için doğrulanmalı.
4. **Opsiyonel:** 2.8 (mail), 3.2, 4.1, 4.2, 3.3 (parent UI), 5.1/6.1 (grafikler) sonraki iterasyonda planlanabilir.

---

**Özet:** Yönetici isteklerinden yapılabilir ve öncelikli olanlar büyük oranda tamamlandı; DB migration’ı ve kod yapısı proje standartlarına uygun. Yukarıdaki maddeler tamamlandığında canlıya çıkış için teknik taraf hazır kabul edilebilir.
