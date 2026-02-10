# AccessManager — Ürün Vizyonu ve Yol Haritası

## Neredeyiz, ne yapıyoruz?

**Hedef kitle:** Binlerce çalışanı olan, çok sayıda departmanı bulunan şirketler. Yetki ve erişim süreçlerinde angarya işler bugün çoğunlukla **Bilgi İşlem (IT)** departmanının sırtında.

**AccessManager’ın amacı:** Bu dünyada hayatı kolaylaştırmak — hem IT’in iş yükünü azaltmak hem de personel yaşam döngüsü ve yetki süreçlerini tek yerden, izlenebilir ve onaylı şekilde yönetmek.

---

## Şu an ne var? (Mevcut kapsam)

| Alan | Ne yapıyor? |
|------|-------------|
| **Personel & departman** | Personel kaydı, departman, rol, yönetici atama. |
| **Sistem envanteri** | Hangi sistemlere (Jira, Bitbucket, ERP vb.) yetki verilebiliyor, sistem sahibi kim? |
| **Roller ve yetkiler** | Rol tanımı, role bağlı yetkiler (permission). |
| **Yetki talepleri** | Talep oluşturma, onay zinciri (Yönetici → Sistem sahibi → IT), “Uygulandı” aşaması. |
| **İşe giriş (Onboarding)** | Yeni personel ekleme, departman/rol/yönetici atama. |
| **İşten çıkış (Offboarding)** | Personel seçimi, erişimlerin kapatılması. |
| **Raporlar** | Sisteme göre aktif erişim, işten çıkanlar, rol dışı yetkiler, süresi dolacak yetkiler. |
| **Denetim kaydı** | Kim, ne zaman, ne yaptı — tam audit trail. |

Yani: **tek merkezden tanım + onay süreci + raporlama** var. Asıl angarya ise hâlâ “bu onaylanan şeyi gerçek sistemlerde yapmak” ve “her şeyi hatırlamak / takip etmek” kısmında.

---

## Bu dünyada hayatı nasıl daha da kolaylaştırabiliriz?

Aşağıdaki başlıklar, IT’in ve organizasyonun angaryasını azaltmaya ve süreçleri güvenli tutmaya yönelik öneriler. Öncelik senin kullanım senaryona göre değiştirilebilir.

---

### 1. Otomasyon ve entegrasyonlar (IT’in elini azaltmak)

- **Gerçek sistemlere bağlantı:** “Uygulandı” denince sadece AccessManager’da işaretlenmesi değil, mümkünse **Jira, Bitbucket, Google Workspace, Slack, ERP** vb. sistemlerde gerçekten hesap açma / grup ekleme / yetki verme (API veya provisioning). IT sadece istisnaları ve hataları yönetsin.
- **İşten çıkış otomasyonu:** Offboarding onaylandığında tüm sistemlerdeki erişimlerin **tek tıkla veya zamanlanmış** kapatılması; “unutulan sistem” kalmaması.
- **AD / LDAP / SSO:** Yeni personel = otomatik “bu kişi için hesap açılsın” veya mevcut AD/SSO ile senkron (e-posta, gruplar). IT’in her seferinde manuel hesap açması azalır.

**Özet:** Onay süreci AccessManager’da kalsın; “uygulama” kısmı mümkün olduğunca otomatikleşsin.

---

### 2. Şablonlar ve standartlar (tekrarları azaltmak)

- **Rol şablonları:** “Backend Developer”, “Product Manager”, “Finans Uzmanı” gibi roller için **varsayılan sistem + yetki seti**. İşe girişte rol seçilince, “bu role önerilen erişimler” otomatik listelensin; tek tıkla talep paketi oluşturulsun.
- **Departman / rol bazlı varsayılanlar:** “Bu departmandaki herkese şu sistemler önerilsin” — yeni gelen için tek seferde çoklu talep.
- **Onay şablonları:** Talep türüne göre farklı onay zincirleri (örn. prod erişimi → 2 onay, standart erişim → 1 onay). IT’in “bu nasıl onaylanacak?” kararını her seferinde vermesi gerekmesin.

**Özet:** Aynı işi her seferinde sıfırdan tanımlamak yerine şablondan üretmek.

---

### 3. Süre ve geçici yetki (unutmayı azaltmak)

- **Süre sınırlı talepler:** Zaten “bitiş tarihi” var; **süre dolunca otomatik pasifleştirme / revoke** (arka planda job). IT’in takvimle “bu yetkinin süresi doldu mu?” diye takip etmesi gerekmesin.
- **Geçici yetki talepleri:** Örn. 1 haftalık prod erişimi — süre bitince otomatik kaldırma, uyarı e-postası.
- **Süresi dolacak yetkiler:** 30 / 7 gün kala **e-posta veya bildirim** (yönetici + personel, opsiyonel IT). “Süre uzatılsın mı?” kararı erkenden verilsin.

**Özet:** Süreleri sistem takip etsin; insan “unutmasın”.

---

### 4. Bildirimler ve hatırlatmalar (gecikmeleri azaltmak)

- **Onay bekleyen talepler:** Yönetici ve sistem sahibine **e-posta / Slack** bildirimi — “Talebiniz bekliyor” denilsin, onaylar gecikmesin.
- **Offboarding checklist:** Son iş günü yaklaşırken “Bu kişinin tüm erişimleri kapatıldı mı?” özeti; eksik varsa IT’e uyarı.
- **Yeni talep / onaylandı / reddedildi:** Talep sahibine bildirim; “ne oldu?” sorusu azalsın.

**Özet:** Süreç aksiyon gerektirdiğinde ilgili kişi bilgilensin.

---

### 5. Self-service ve delegasyon (IT’e düşen ad hoc işi azaltmak)

- **Çalışan self-service:** “Bana atanmış yetkiler neler?” sayfası; gereksizse “bu erişimi iptal et” talebi açabilsin. IT’e “şu hesabımı kapat” e-postası azalır.
- **Yönetici delegasyonu:** Yönetici, ekibinin **şablona uygun** standart taleplerini onaylayabilsin; sadece istisnai / riskli talepler IT’e gitsin.
- **Vekil atama:** Yönetici yokken (tatil, hastalık) yerine vekilin onay verebilmesi — süreç takılı kalmasın.

**Özet:** Basit ve standart işler çalışan/yönetici tarafında kalsın; IT “özel durumlar”a odaklansın.

---

### 6. Raporlama ve uyumluluk (denetim ve kontrol)

- **Excel / PDF export:** Raporlar ve denetim kaydı dışa aktarılsın; denetçi ve compliance için kullanılsın.
- **Rol dışı yetkiler aksiyonu:** Exception report’taki “rol dışı yetkiler” için **toplu revoke** veya “role taşı / rolü güncelle” seçenekleri — sadece rapor değil, aksiyon.
- **SoD (Görev ayrımı):** Kritik rollerde çakışma uyarısı (aynı kişi hem onaylayan hem uygulayan olmasın).
- **Periyodik erişim incelemesi:** Yöneticiye “ekibinizin erişimleri — onaylıyor musunuz?” listesi; yılda bir / çeyreklik review.

**Özet:** Hem “kim neye erişiyor” görünsün hem de toplu düzeltme ve uyumluluk kolaylaşsın.

---

### 7. IT ve operasyon için pratik işler

- **Toplu güncelleme:** Departman taşınması, birim birleşmesi vb. — “Bu departmandaki herkesin rolü / erişimi şu şablona göre güncellensin” (dikkatli ve onaylı kullanım).
- **Sistem sahibi net olsun:** Her sistemin “sistem sahibi” (mümkünse iş birimi); yetki ihtiyacı ve onay onlarda, IT “uygulama” aşamasında. AccessManager’da bu zaten var; kullanımı yaygınlaştırmak.
- **Bekleyen işler panosu:** IT için “bugün uygulanacak talepler”, “süresi dolan yetkiler”, “offboarding tamamlanacak kişiler” özeti — tek ekrandan günlük iş takibi.

**Özet:** IT’in günlük işi öngörülebilir ve toplu işlerle hafiflesin.

---

## Önerilen öncelik sırası (tartışmaya açık)

| Öncelik | Ne | Neden |
|--------|----|-------|
| **1** | Bildirimler (e-posta / entegrasyon) | Onaylar ve süreç gecikmesin; en hızlı etki. |
| **2** | Süre dolunca otomatik pasifleştirme + “süresi dolacak” uyarısı | Unutulan yetkiler azalsın; güvenlik ve uyumluluk. |
| **3** | Rol / departman şablonları | Aynı işi tekrar tekrar yapmamak; ölçeklenebilirlik. |
| **4** | Self-service: “Yetkilerim” + iptal talebi | IT’e gelen ad hoc istekler azalsın. |
| **5** | Gerçek sistem entegrasyonları (Jira, Bitbucket vb.) | “Uygulandı” = gerçekten uygulansın; IT eli minimuma insin. |
| **6** | Offboarding otomasyonu + checklist | İşten çıkışta “bir şey unutulmasın”. |
| **7** | Rapor export (Excel/PDF) + rol dışı yetki aksiyonu | Denetim ve compliance. |

---

## Kısa özet

- **Şu an:** Tek merkezden tanım, onay akışı, raporlama ve denetim kaydı var; angarya hâlâ “elle uygulama” ve “her şeyi hatırlama”da.
- **Hedef:** Bildirimler, süre otomasyonu, şablonlar ve self-service ile süreç hızlansın; entegrasyonlar ve offboarding otomasyonu ile IT’in elle yaptığı iş azalsın; raporlama ve aksiyonlarla uyumluluk ve kontrol güçlensin.

Bu doküman, projenin **ürün vizyonu ve yol haritası** için ortak bir referans olarak kullanılabilir; senaryona göre maddeler eklenip çıkarılabilir veya öncelikler değiştirilebilir.
