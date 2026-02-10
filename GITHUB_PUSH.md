# Access Manager – GitHub'a Yükleme Adımları

## 1. Proje klasöründe Git başlat (henüz yapılmadıysa)

PowerShell veya Git Bash’te:

```powershell
cd c:\Projects\arabam-access-manager
git init
```

## 2. Tüm dosyaları ekle ve ilk commit

```powershell
git add .
git status
git commit -m "Initial commit: Access Manager Faz 1"
```

## 3. GitHub’da yeni repo oluştur

1. https://github.com/new adresine git.
2. **Repository name:** `arabam-access-manager` (veya istediğin isim).
3. **Public** seç.
4. **README, .gitignore, license ekleme** – projede zaten .gitignore var.
5. **Create repository**’e tıkla.

## 4. Remote ekle ve push

GitHub repo sayfasında çıkan komutlardan **“push an existing repository”** kısmını kullan. Kendi kullanıcı adınla:

```powershell
git remote add origin https://github.com/SENIN_GITHUB_KULLANICI_ADIN/arabam-access-manager.git
git branch -M main
git push -u origin main
```

Örnek (kullanıcı adın `emreturkoglu` ise):

```powershell
git remote add origin https://github.com/emreturkoglu/arabam-access-manager.git
git branch -M main
git push -u origin main
```

---

## .gitignore

Proje kökünde `.gitignore` var. `bin/`, `obj/`, `.vs/`, `packages/` vb. commit’e girmeyecek.

---

## Sonraki adım: Google Cloud Run

Repo GitHub’da olduktan sonra:

1. Google Cloud Console → **Cloud Run** → **Create Service**.
2. **Continuously deploy from a repository** → GitHub’ı bağla, bu repoyu seç.
3. Branch: `main`, Build: **Dockerfile** veya **Buildpack** (ASP.NET Core için uygun olanı seç).
4. Cloud SQL (PostgreSQL) bağlantısı için Cloud Run’a **Cloud SQL connection** ekle ve connection name’i env’e ver.

İstersen Cloud Run + Dockerfile adımlarını da ayrı bir dokümanda yazabilirim.
