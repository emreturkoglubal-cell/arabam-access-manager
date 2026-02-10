# Gerçek Dünyada IT Süreci — Access Manager’da Nerede, Nasıl?

Bu doküman, **gerçek hayatta** bilgi işlem müdürü / IT direktörünün bu süreçleri **nereden, nasıl** yönettiğini ve **Access Manager’da** bunun **hangi ekran, hangi tuş** ile yapıldığını tek tek eşleştirir.

---

## 1. Gerçek dünyada bu süreç nasıl yönetiliyor?

Çoğu şirkette bugün şöyle işliyor:

- **Excel / liste:** İşe girenler, ayrılanlar, kimde hangi yetki var — listeler Excel’de veya paylaşılan bir dosyada tutulur.
- **E-posta / talep:** “X kişisine Jira yetkisi verin”, “Y ayrıldı tüm hesaplar kapatılsın” — talepler e-postayla veya ticket sistemiyle gelir.
- **Manuel işlem:** IT çalışanı her sisteme (AD, Jira, Bitbucket, mail, VPN vb.) tek tek girip hesap açar / yetki verir / kapatır. Kimin neye ihtiyacı olduğu bazen e-postada, bazen ağızdan söylenir.
- **Onay:** Bazen “yönetici onayı” e-postayla alınır; bazen hiç yazılı kalmadan “sözlü onay” ile işlem yapılır. Denetimde “kim, ne zaman, neye onay verdi” net değildir.
- **Tek merkez yok:** Personel listesi bir yerde, yetkiler başka sistemlerde, işe giriş/çıkış takibi başka bir yerde. IT müdürü “bugün kim işe başlıyor, kimin yetkileri kapatılacak?” diye bir ekrandan bakamaz; birkaç kaynağı bir araya getirmek zorunda kalır.

Yani süreç **dağınık**, **elle** ve **izlenebilirliği zayıf**.

---

## 2. Access Manager’da aynı süreç nerede, nasıl yönetiliyor?

Aşağıda dört ana senaryo var: **işe giriş**, **işten çıkış**, **yeni yetki verme**, **yetki çıkarma**. Her biri için önce “gerçek dünyada ne yapılıyor”, sonra “Access Manager’da nereden ve hangi tuşla yapılıyor” yazıyor.

---

### Senaryo A: Yeni personel işe girdi (onboarding)

| Gerçek dünyada | Access Manager’da |
|----------------|--------------------|
| HR/yonetici “X pazartesi işe başlıyor” der. IT, Excel’e veya listeye ekler; ardından maile, AD’ye, Jira’ya vb. hesap açar. | **Sol menü → İşe Giriş** |
| “Hangi departman, hangi rol, yönetici kim?” bilgisi e-postada veya formda gelir. | Aynı ekranda **Departman, Rol, Yönetici** seçilir; **Sicil No, Ad, Soyad, E-posta** girilir. |
| IT bu bilgiyle tek tek sistemlerde işlem yapar (elle). | **“Kaydet” / “İşe girişi tamamla”** tuşuna basılır → Personel kaydı oluşur. (Gerçek sistemlere otomatik yetki verme şu an yok; o ileride entegrasyonla gelir. Ama **kim işe girdi, hangi rol/departman** tek merkezde kayıtlı olur.) |
| Donanım (bilgisayar, telefon) verilir; bazen ayrı bir zimmet listesinde tutulur. | Aynı kişi **Personel** listesinde görünür. Donanım vermek için **Donanım & Zimmet** ekranına gidilir, ilgili personel seçilip **Zimmetle** denir. |

**Özet:** IT müdürü (veya yetkili kullanıcı) **İşe Giriş** sayfasından yeni personeli sisteme ekler; departman, rol, yönetici burada seçilir. Donanım vermek ayrı olarak **Donanım & Zimmet** üzerinden yapılır.

---

### Senaryo B: Personel işten ayrıldı (offboarding)

| Gerçek dünyada | Access Manager’da |
|----------------|--------------------|
| HR/yonetici “Y’nin son günü Cuma” der. IT, tüm hesapları kapatması gerektiğini bilir; hangi sistemlerde hesabı var bazen Excel’den bazen “bildiği kadar” takip edilir. | **Sol menü → İşten Çıkış** |
| IT her sisteme tek tek girip yetkileri kapatır; bazen bir sistem unutulur. | **Ayrılan personel** dropdown’dan seçilir, **çıkış tarihi** girilir, **“İşten çıkışı tamamla”** (veya benzeri) tuşuna basılır. |
| | Sistem, o personelin **tüm yetkilerini “pasif”** yapar; personel **“İşten ayrıldı”** durumuna geçer. (Gerçek sistemlerde hesap kapatma yine entegrasyonla olur; ama **kimin ne zaman ayrıldığı ve yetkilerin sistem tarafında pasif olması** bu ekrandan yönetilir.) |
| Donanım iade alınır; bazen Excel’de “iade alındı” işaretlenir. | **Donanım & Zimmet** ekranından ilgili cihazlar bulunur, **“İade al”** ile zimmet kapatılır. |

**Özet:** IT müdürü **İşten Çıkış** sayfasından ayrılan personeli seçip çıkış tarihini girerek süreci kayda alır; yetkiler tek tıkla pasiflenir. Donanım iadesi **Donanım & Zimmet**’ten yapılır.

---

### Senaryo C: Mevcut personele yeni yetki verilecek (talep → onay → uygulama)

| Gerçek dünyada | Access Manager’da |
|----------------|--------------------|
| Çalışan veya yönetici “Z’ye Bitbucket yazma yetkisi verin” diye e-posta/ticket açar. | **Sol menü → Yetki Talepleri** |
| Yönetici e-postayla “onaylıyorum” der (veya sözlü). | Talep oluşturulunca durum **“Yönetici onayı bekliyor”** olur. **Yetki Talepleri** listesinden ilgili talebe tıklanır → **Talep Detay** sayfası açılır. |
| Sistem sahibi (örn. Bitbucket’tan sorumlu kişi) bazen ayrıca onaylar. | Eğer tanımlıysa **Sistem sahibi onayı** adımı da listede görünür; onaylayan kişi aynı detay sayfasında **“Onayla”** veya **“Reddet”** tuşuna basar. |
| IT, onaylanan talebi alır ve Jira/Bitbucket vb. sistemde elle yetki verir. | Son adım **IT onayı**. IT yetkilisi aynı **Talep Detay** sayfasında **“Onayla”** tuşuna basar. Onaylanınca sistem **otomatik** olarak talebi **“Uygulandı”** yapar ve ilgili personel için **yetki kaydını** (PersonnelAccess) oluşturur. (Gerçek Jira/Bitbucket’ta hesap açma şu an yok; o entegrasyonla gelir. Ama **kimin hangi yetkiye onaylandığı ve “uygulandı” bilgisi** burada tutulur.) |

**Özet:**  
- **Talep açma:** Yetki Talepleri → **Yeni Talep** → personel, sistem, yetki türü, gerekirse sebep/süre.  
- **Onaylama:** Yetki Talepleri → ilgili talep → **Detay** → **Onayla** / **Reddet**.  
- **“Uygulandı”:** IT son onayı verdiğinde sistem otomatik “Uygulandı” yapıyor; ekstra bir “Uygula” tuşu yok. Tüm onay zinciri ve kimin onayladığı **Denetim Kaydı**’nda izlenebilir.

---

### Senaryo D: Yetki çıkarılacak (revoke)

| Gerçek dünyada | Access Manager’da |
|----------------|--------------------|
| “Z’nin Bitbucket yetkisi kaldırılsın” e-postayla/ticket ile gelir; IT ilgili sistemde yetkiyi kaldırır. | Şu an Access Manager’da **“yetki çıkar”** için ayrı bir ekran yok. Yapılabilecekler: **Personel → ilgili kişi → Detay** sayfasında **Aktif Yetkiler** listesini görmek; yetki kaldırma işlemi ileride buradan “Yetki kaldır” butonu veya ayrı bir **Yetki Yönetimi** ekranıyla eklenebilir. |
| İşten çıkışta tüm yetkiler kapatılır. | **İşten Çıkış** ekranından personel işten çıkarıldığında **tüm yetkileri otomatik pasif** olur. |

**Özet:** Toplu yetki çıkarma = **İşten Çıkış**. Tekil yetki çıkarma için ileride ayrı bir ekran/buton eklenebilir; şu an personel detayında “aktif yetkiler” görüntülenebilir.

---

## 3. Kim nereden girer, hangi tuşlar?

| Rol | Erişebildiği menüler (özet) | Bu süreçte yaptığı |
|-----|------------------------------|---------------------|
| **Admin / Manager (IT müdürü, yetkili)** | Kontrol Paneli, Personel, Departmanlar, Sistem Envanteri, Donanım & Zimmet, Roller, **İşe Giriş**, **İşten Çıkış**, Yetki Talepleri, Raporlar | **İşe Giriş** ile yeni personel ekler; **İşten Çıkış** ile ayrılanı işaretler; **Yetki Talepleri → Detay** üzerinden onaylar (IT onayı); **Donanım & Zimmet** ile zimmet verir/iade alır. |
| **User (çalışan)** | Yetki Talepleri (kendi taleplerini açabilir / listeleyebilir) | **Yeni Talep** ile “bana şu sistemde yetki verin” talebini açar; onay süreci yönetici ve IT’e gider. |
| **Auditor** | Raporlar, Denetim Kaydı | Kim ne zaman ne onaylamış, kim işe girmiş/çıkmış — hepsini **Denetim Kaydı** ve raporlardan izler. |

---

## 4. Tek cümleyle: “Bir tuşa basıp nasıl yönetilecek?”

- **İşe giren biri:** Sol menüden **İşe Giriş** → formu doldur → **Kaydet**. Personel ve rol/departman tek merkezde kayıtlı; donanım için ayrıca **Donanım & Zimmet → Zimmetle**.  
- **İşten ayrılan biri:** **İşten Çıkış** → personeli seç, çıkış tarihini gir → **Tamamla**. Yetkiler pasiflenir; donanım **Donanım & Zimmet**’ten iade alınır.  
- **Yeni yetki verilecek:** **Yetki Talepleri → Yeni Talep** ile talep açılır → **Talep Detay**’ta sırayla **Yönetici / Sistem sahibi / IT** **Onayla** tuşuna basar → IT onayında sistem otomatik **“Uygulandı”** yapar.  
- **Yetki çıkarılacak (toplu):** **İşten Çıkış** ile kişi ayrıldığında tüm yetkiler kapanır. Tekil yetki çıkarma ileride ek ekranla gelebilir.

Bu sayede **gerçek dünyadaki** “Excel + e-posta + elle sistemlere girme” süreci, Access Manager’da **tek yerden, hangi menüden hangi tuşa basılacağı belli** bir akışa dönüşüyor; denetim ve raporlama da aynı sistemde tutuluyor.
