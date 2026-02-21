# Cloud Run — Gerekli Ortam Değişkenleri

Canlıda AI'ın **dosya okuyabilmesi** (read_file) ve **commit/push** yapabilmesi için aşağıdaki değişkenlerin Cloud Run **Variables & Secrets** ekranında tanımlı olması gerekir.

## Zorunlu (path — read_file için)

| Name (Cloud Run'da tam böyle yaz) | Value | Açıklama |
|-----------------------------------|--------|----------|
| `Git__RepoPath` | `/app/repo` | Repo kökü. Dockerfile'da kaynak `/app/repo`'ya kopyalanıyor. **İki alt çizgi** (`__`) kullan; .NET'te `:` buna karşılık gelir. |
| `CodeContext__BasePath` | `/app/repo` | Proje yapısı listesinin alınacağı dizin. `read_file` ile aynı repo olmalı. |

Bu ikisi **yoksa** canlıda `read_file` hep "Git:RepoPath yapılandırılmamış" veya "Dosya bulunamadı" döner.

## Diğer (zaten kullandığın)

- `ConnectionStrings__DefaultConnection` — Veritabanı (iki alt çizgi).
- `OpenAI__ApiKey` — OpenAI API anahtarı. **İki alt çizgi** kullan (`OpenAI:ApiKey` → `OpenAI__ApiKey`). Tek alt çizgi (`OpenAI_ApiKey`) .NET tarafından bu key’e bağlanmaz.
- `Git__Token` — GitHub PAT (push için). Key `Git:Token` ise env adı `Git__Token` olmalı.

## Özet

Cloud Run → **Variables & Secrets** → **Add variable** ile ekle:

1. **Name:** `Git__RepoPath` — **Value:** `/app/repo`
2. **Name:** `CodeContext__BasePath` — **Value:** `/app/repo`

Deploy sonrası uygulama log’unda şunu görmelisin:  
`HasSource = true`.  
`HasSource = false` ise container’da `/app/repo` altında kaynak yok demektir (Dockerfile/build kontrol et).
