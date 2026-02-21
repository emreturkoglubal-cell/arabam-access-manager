# Cloud Run — Gerekli Ortam Değişkenleri

Canlıda AI'ın **dosya okuyabilmesi** (read_file), **apply_diff** ve **commit/push** yapabilmesi için aşağıdaki değişkenlerin Cloud Run **Variables & Secrets** ekranında tanımlı olması gerekir.

## Ortam: Mutlaka Production kullan

Canlıda **appsettings.Production.json** kullanılsın diye Cloud Run’da şu değişkeni **mutlaka** ekle:

| Name | Value | Açıklama |
|------|--------|----------|
| **`ASPNETCORE_ENVIRONMENT`** | **`Production`** | .NET Core buna göre `appsettings.Production.json` yükler. Bu yoksa veya `Development` ise canlıda yanlış ayarlar (lokal path vb.) kullanılabilir. |

Bu değişkeni **en üstte** veya ilk sıralarda tanımlaman iyi olur; diğer ayarlar buna göre yüklenir.

**appsettings.json / appsettings.Development.json ile birebir eşitleme yapma.** Development’ta lokal path (örn. `c:\Projects\...`) kalmalı; Production’da canlı path’ler (`/tmp/repo`) kalmalı. Sadece canlıda **ASPNETCORE_ENVIRONMENT=Production** vererek doğru dosyanın (appsettings.Production.json) kullanılmasını sağla.

## Önemli: Cloud Run’da /app salt okunur

Cloud Run’da container’ın `/app` dizini **salt okunur**. Bu yüzden `apply_diff` (dosya yazma) ve `git commit/push` **başarısız** olur. Çözüm: Repo’yu **yazılabilir** bir dizinde kullanmak.

**Git:RepoPath** ve **CodeContext:BasePath** değerini **`/tmp/repo`** yap. Uygulama açılışta `/app/repo` içeriğini `/tmp/repo`’ya kopyalar; tüm okuma/yazma ve push orada çalışır.

## Zorunlu (path — read_file + apply_diff + push için)

| Name (Cloud Run'da tam böyle yaz) | Value | Açıklama |
|-----------------------------------|--------|----------|
| `Git__RepoPath` | **`/tmp/repo`** | Repo kökü. Açılışta `/app/repo` buraya kopyalanır; yazılabilir olduğu için apply_diff ve push çalışır. |
| `CodeContext__BasePath` | **`/tmp/repo`** | Proje yapısı listesi bu dizinden alınır. `read_file` ile aynı repo olmalı. |

Sadece `/app/repo` kullanırsan read_file çalışır ama **apply_diff ve push çalışmaz** (dosya yazma hatası veya commit hatası alırsın).

## Diğer (zaten kullandığın)

- `ConnectionStrings__DefaultConnection` — Veritabanı (iki alt çizgi).
- `OpenAI__ApiKey` — OpenAI API anahtarı. **İki alt çizgi** kullan (`OpenAI:ApiKey` → `OpenAI__ApiKey`). Tek alt çizgi (`OpenAI_ApiKey`) .NET tarafından bu key’e bağlanmaz.
- `Git__Token` — GitHub PAT (push için). Key `Git:Token` ise env adı `Git__Token` olmalı.

## Özet

Cloud Run → **Variables & Secrets** → ekle/güncelle (sıra önemli değil):

| Name | Value |
|------|--------|
| **`ASPNETCORE_ENVIRONMENT`** | **`Production`** |
| `Git__RepoPath` | `/tmp/repo` |
| `CodeContext__BasePath` | `/tmp/repo` |
| `ConnectionStrings__DefaultConnection` | (veritabanı connection string) |
| `OpenAI__ApiKey` | (OpenAI anahtarın) |
| `Git__Token` | (GitHub PAT) |

Deploy sonrası log’da: `Cloud Run: /app/repo yazılabilir olması için /tmp/repo'ya kopyalandı` ve `HasSource = true` görmelisin. Ortamın Production olduğunu doğrulamak için log’da `Application started` veya benzeri satırda environment bilgisi de çıkabilir (isteğe bağlı: startup’ta `EnvironmentName` loglayabilirsin).
