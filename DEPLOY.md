# Access Manager – Google Cloud Run deploy

Bu proje **main** branch’e push edildiğinde **Google Cloud Build** ile otomatik build alıp **Cloud Run**’a deploy edilecek şekilde ayarlanabilir.

## Repo’daki dosyalar

| Dosya | Açıklama |
|-------|----------|
| `Dockerfile` | ASP.NET Core uygulamasını 8080 portunda çalıştıran multi-stage build. |
| `.dockerignore` | Build context’i küçük tutar; bin/obj, .git, gereksiz dosyalar hariç. |
| `cloudbuild.yaml` | Cloud Build adımları: Docker build → Artifact Registry push → Cloud Run deploy. |

## İlk kurulum (bir kez)

### 1. GCP projesi ve API’ler

- [Google Cloud Console](https://console.cloud.google.com/) → proje seç / oluştur.
- Şu API’leri aç:
  - Cloud Build
  - Cloud Run
  - Artifact Registry
  - Resource Manager  

  Hızlı link: [API’leri etkinleştir](https://console.cloud.google.com/flows/enableapi?apiid=cloudbuild.googleapis.com,run.googleapis.com,artifactregistry.googleapis.com,cloudresourcemanager.googleapis.com)

### 2. Artifact Registry repository

Aynı bölgede (ör. `europe-west1`) bir Docker repository oluştur:

```bash
gcloud artifacts repositories create access-manager \
  --repository-format=docker \
  --location=europe-west1 \
  --description="Access Manager Docker images"
```

`cloudbuild.yaml` içindeki `_REPOSITORY` değeri (`access-manager`) buna uyumlu.

### 3. Cloud Build – GitHub bağlantısı

- [Cloud Build Triggers](https://console.cloud.google.com/cloud-build/triggers)
- **Create trigger**
  - **Event:** Push to a branch  
  - **Source:** GitHub repo’nu bağla (ilk seferde “Connect repository” ile yetkilendir)
  - **Branch:** `^main$`
  - **Configuration:** “Cloud Build configuration file (yaml or json)”
  - **Location:** `cloudbuild.yaml` (repo kökünde)
  - **Region:** Örn. `europe-west1` (Cloud Run ile aynı bölge mantıklı)

İsteğe bağlı: Trigger’da **Substitution variables** ile `_REGION`, `_SERVICE_NAME`, `_REPOSITORY` değerlerini override edebilirsin.

### 4. Cloud Build servis hesabı yetkileri

[Cloud Build Settings → Permissions](https://console.cloud.google.com/cloud-build/settings) üzerinden (varsayılan veya kullandığın) Cloud Build servis hesabına şu roller verilmeli:

- **Cloud Run Admin** – deploy için  
- **Artifact Registry Writer** – image push için  
- **Storage / Logging** – build log ve artifact’lar için (genelde zaten açık)

Detay: [Cloud Build – Deploy Cloud Run](https://cloud.google.com/build/docs/deploying-builds/deploy-cloud-run).

## Otomatik deploy akışı

1. `main` branch’e push (merge veya doğrudan push).
2. Cloud Build trigger tetiklenir.
3. `cloudbuild.yaml` çalışır:
   - `Dockerfile` ile image build edilir.
   - Image Artifact Registry’ye push edilir (tag: `$COMMIT_SHA` ve `latest`).
   - Aynı image Cloud Run servisine deploy edilir.
4. Yeni revizyon yayına alınır; servis URL’i aynı kalır.

## Lokal test (opsiyonel)

```bash
# Proje kökünde
docker build -t access-manager:local .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production access-manager:local
# http://localhost:8080
```

## Ortam / connection string

Cloud Run’da **PostgreSQL connection string** ve diğer hassas ayarlar için:

- **Secret Manager** kullan veya  
- Cloud Run servisinde **Environment variables** / **Secrets** alanından bağla.

`appsettings.Production.json` veya env ile `ConnectionStrings__DefaultConnection` set edilmelidir; aksi halde uygulama veritabanına bağlanamaz.

## Notlar

- Varsayılan `cloudbuild.yaml` içinde `--allow-unauthenticated` var; servis herkese açık. Sadece iç kullanım istiyorsan trigger/build’den sonra Cloud Run’da “Require authentication” aç.
- İlk deploy’dan önce Artifact Registry repository ve trigger’ın bölgesi (`_REGION`) uyumlu olmalı.

Bu adımlarla **main**’e her push’ta otomatik deploy çalışır.
