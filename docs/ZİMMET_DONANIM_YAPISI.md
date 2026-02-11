# Zimmet (Donanım) Yapısı — DB, Backend, Frontend

Bu doküman, donanım envanteri ve zimmete alma / iade akışının **veritabanı**, **backend** ve **frontend** tarafında nerede ve nasıl tanımlı olduğunu özetler.

---

## 1. Veritabanı (PostgreSQL)

**Dosyalar:** `database/01_create_tables.sql`, `database/02_seed_data.sql`

### Tablolar

| Tablo | Açıklama |
|-------|----------|
| **assets** | Donanım envanteri (laptop, telefon, monitör vb.). `asset_type`, `name`, `serial_number`, `brand_model`, `status` (Available/Assigned/InRepair/Retired), `notes`, `purchase_date`. |
| **asset_assignments** | Zimmet kayıtları. Hangi donanım (`asset_id`) kime (`personnel_id`) ne zaman verildi, kim verdi (`assigned_by_user_id/name`), ne zaman iade edildi (`returned_at`), iade durumu (`return_condition`), notlar. |
| **asset_assignment_notes** | Zimmet kaydına eklenen notlar (birden fazla). `asset_assignment_id`, `content`, `created_at`, `created_by_user_id`, `created_by_user_name`. |

### Seed

- `02_seed_data.sql` içinde örnek `assets` (dizüstü, telefon, monitör) ve `asset_assignments` (personellere atamalar) tanımlı.
- Örnek bir `asset_assignment_notes` kaydı da eklenir.

---

## 2. Backend

### Domain (Entities)

- **Asset**: `Id`, `AssetType`, `Name`, `SerialNumber`, `BrandModel`, `Status`, `Notes`, `PurchaseDate`, `CreatedAt`.
- **AssetAssignment**: `Id`, `AssetId`, `PersonnelId`, `AssignedAt`, `AssignedByUserId`, `AssignedByUserName`, `ReturnedAt`, `ReturnCondition`, `Notes`.
- **AssetAssignmentNote**: `Id`, `AssetAssignmentId`, `Content`, `CreatedAt`, `CreatedByUserId`, `CreatedByUserName`.

### Repositories (Dapper / Npgsql)

- **IAssetRepository / AssetRepository**: `GetAll`, `GetByStatus`, `GetByType`, `GetById`, `Insert`, `Update`, `Delete`.
- **IAssetAssignmentRepository / AssetAssignmentRepository**: `GetByAssetId`, `GetByPersonnelId`, `GetActiveByAssetId`, `GetById`, `Insert`, `SetReturned`, `GetNotesByAssignmentId`, `AddNote`.

### Application (IAssetService / AssetService)

- **Okuma:** `GetAll`, `GetByStatus`, `GetByType`, `GetById`, `GetActiveAssignmentsByPersonnel`, `GetAssignmentHistoryByAsset`, `GetActiveAssignmentForAsset`, `GetAssignmentById`, `GetNotesForAssignment`.
- **Yazma:** `Create`, `Update`, `Delete`, `Assign`, `Return`, `AddNoteToAssignment`.

### Controllers

- **AssetsController**
  - `Index`: Donanım listesi, filtre (durum/tür), zimmette kim var, Düzenle / Sil / Zimmetle / İade al.
  - `Create`, `Edit`, `Delete`: Donanım CRUD (Admin).
  - `Assign` (GET/POST): Donanımı personel seçerek zimmete ver.
  - `Return` (GET/POST): Zimmet kaydını iade et.
- **PersonnelController**
  - `Detail`: Personelin zimmetteki donanımları (`AssetAssignments`), her zimmet için notlar (`AssignmentNotes`).
  - `AddZimmetNote` (POST): Zimmet kaydına not ekler.

---

## 3. Frontend (Views)

### Donanım & Zimmet (Assets)

| View | İşlev |
|------|--------|
| **Index** | Liste (tür, ad, seri no, marka/model, durum, zimmette kim, zimmet tarihi). Düzenle / Sil (Admin), Zimmetle / İade al. |
| **Create** | Yeni donanım formu. |
| **Edit** | Donanım düzenleme. |
| **Delete** | Silme onayı. |
| **Assign** | Personel seçip zimmete alma formu. |
| **Return** | İade formu (iade durumu, not). |

### Personel detay (Zimmetteki donanım)

- **Personnel/Detail** içinde **"Zimmetteki donanım"** kartı:
  - Personelin aktif zimmetleri listelenir (donanım adı, tür, zimmet tarihi, açıklama).
  - Her zimmet için **notlar** listelenir (tarih, kim yazdı, içerik).
  - Her zimmet için **"Not ekle"** formu (`AddZimmetNote`).
  - Zimmette donanım yoksa **"Donanım & Zimmet"** linki ile envanter sayfasına gidilir.

---

## 4. Akış özeti

1. **Zimmete alma:** Donanım & Zimmet → ilgili donanım satırında **Zimmetle** → personel seç → kaydet. `asset_assignments` ve asset `status` güncellenir.
2. **İade:** Aynı sayfada **İade al** → iade durumu/not → kaydet. `returned_at` dolar, asset tekrar Available olur.
3. **Personel detayda zimmet:** Personel detay sayfasında "Zimmetteki donanım" kartında atamalar ve notlar görünür; yeni not **AddZimmetNote** ile eklenir.

Tüm bu yapı **DB + backend + frontend** ile projede tanımlıdır; connection string ile PostgreSQL kullanıldığında tam çalışır.
