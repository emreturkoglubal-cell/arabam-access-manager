# İki Faktörlü Doğrulama (TOTP / Google Authenticator) — Açıklama ve Seçenekler

## Arabamcom Admin’deki yapı

**Arabam.Admin** projesinde giriş **Keycloak SSO** ile yapılıyor:

- Kullanıcı `/login-sso` ile **Keycloak** sunucusuna yönlendiriliyor.
- Şifre + **TOTP (authenticator)** doğrulaması **Keycloak’ın kendi ekranında** yapılıyor (Keycloak sunucusu TOTP’ü yönetiyor).
- `KeyCloakClient.cs` içindeki `CONFIGURE_TOTP`, sadece Keycloak’ta **yeni kullanıcı oluşturulurken** “ilk girişte TOTP kurulsun” anlamına geliyor; TOTP mantığı Admin uygulamasında değil, **Keycloak** tarafında.

Yani: Authenticator’da yazan 6 haneli kod girişi ve doğrulama **Keycloak’ta**; Admin sadece SSO ile token alıyor.

---

## Access Manager’da durum

Access Manager **yerel (local)** kimlik doğrulama kullanıyor:

- Kullanıcılar `AppUser` (mock/veritabanı), şifre `AuthService.ValidateUser` ile kontrol ediliyor.
- Keycloak veya başka bir dış IdP yok.

Bu yüzden **Admin’deki TOTP yapısını “import” edemiyoruz** — orada TOTP, Keycloak sunucusunun parçası; bizde Keycloak yok.

---

## Local ortamda Google Authenticator (TOTP) kullanılabilir mi?

**Evet.** Local olması TOTP kullanmaya engel değil. TOTP (RFC 6238) tamamen **yerel** çalışır:

- Sunucu tarafında her kullanıcı için bir **gizli anahtar (secret)** tutulur.
- Kullanıcı Google Authenticator (veya benzeri) uygulamasında bu secret ile **zaman bazlı 6 haneli kod** üretir.
- Girişte şifre doğrulandıktan sonra ikinci adımda bu kodu gireriz; sunucu aynı secret ile kodu doğrular.

**Kütüphane:** .NET tarafında **OtpNet** (NuGet: `OtpNet`) Google Authenticator ile uyumlu; secret üretmek ve kodu doğrulamak için yeterli.

---

## İki seçenek

### Seçenek 1: Access Manager’a kendi TOTP’ümüzü eklemek (önerilen)

- **Şu anki yapı:** Cookie auth, `AppUser`, `ValidateUser`.
- **Eklenecekler:**
  - `AppUser` (veya kullanıcı tablosu): `TwoFactorEnabled`, `TwoFactorSecret` (şifreli saklansın).
  - Şifre doğru → eğer kullanıcıda 2FA açıksa → giriş tamamlanmaz; geçici bir “2FA bekliyor” bilgisi (cookie veya temp token) ile **ikinci adım sayfasına** yönlendirilir → kullanıcı 6 haneli kodu girer → `OtpNet` ile doğrulanır → tam giriş.
  - İsteğe bağlı: “2FA’yı kur” sayfası (QR kod + secret); kullanıcı bunu bir kez yapınca `TwoFactorEnabled = true` yapılır.
- **Avantaj:** Keycloak’a bağımlılık yok; tamamen bu projede, local ortamda çalışır. Admin’deki “şifre + authenticator kodu” akışının aynısı kullanıcı deneyimi olarak uygulanır.

### Seçenek 2: Access Manager’ı Keycloak’a bağlamak

- Girişi Keycloak SSO’ya taşırız (Admin’deki gibi). O zaman TOTP **yine Keycloak’ta** olur; Access Manager’da ekstra TOTP kodu yazmayız.
- **Dezavantaj:** Keycloak sunucusu, realm ve kullanıcı yönetimi gerekir; mimari değişiklik büyük. “Basit import” değil, entegrasyon projesi olur.

---

## Özet

| Soru | Cevap |
|------|--------|
| Admin’deki authenticator yapısını bu projeye import edebilir miyiz? | **Hayır.** Orada TOTP Keycloak içinde; kod Admin’de değil. |
| Local projede Google Authenticator kullanılabilir mi? | **Evet.** OtpNet ile kendi 2FA adımımızı ekleyebiliriz. |
| Nasıl ekleriz? | Şifre sonrası 2FA açıksa ikinci ekranda 6 haneli kod isteyip OtpNet ile doğrularız; kullanıcıya secret’ı kurması için (QR vs.) bir sayfa sunarız. |

İstersen bir sonraki adımda Access Manager için **Seçenek 1**’i adım adım (AppUser alanları, servis, login 2. adım sayfası, 2FA kurulum sayfası) tasarlayıp kodlayabilirim.
