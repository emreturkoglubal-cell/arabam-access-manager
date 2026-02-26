# arabam AI & Access Manager — Geliştirme Yol Haritası

Bu dokümanda ürünü genişletmek, kullanımı kolaylaştırmak, stabil ve teknolojik olarak güçlendirmek için öneriler listelenmiştir.

---

## 1. Stabilite ve güvenilirlik

| Öneri | Açıklama | Öncelik |
|-------|----------|---------|
| **AI health check** | `/Ai/Health` veya dashboard’da: OpenAI key, Git:RepoPath, dotnet build erişimi kontrolü. Sorun varsa kullanıcıya net mesaj. | Yüksek |
| **Retry + timeout** | OpenAI isteklerinde retry (exponential backoff), timeout ayarı. Uzun tool zincirlerinde kesintiye karşı dayanıklılık. | Yüksek |
| **Push / PR idempotency** | Aynı konuşmada iki kez "Pushla" tıklanırsa veya aynı değişiklik zaten push’landıysa hata verme, anlamlı mesaj dön. | Orta |
| **Diff doğrulama** | `apply_diff` öncesi path ve diff formatı kontrolü; geçersiz diff’te net hata mesajı. | Orta |
| **Audit log (AI aksiyonları)** | Kim, hangi konuşmada push/PR yaptı, hangi dosyalar — audit tablosuna yaz. Raporlama ve güvenlik için. | Yüksek |
| **Hata mesajlarını iyileştir** | API kotası, ağ hatası, build hatası için kullanıcıya Türkçe, anlaşılır açıklama ve “ne yapabilirsin” önerisi. | Yüksek |

---

## 2. Kullanım kolaylığı (UX)

| Öneri | Açıklama | Öncelik |
|-------|----------|---------|
| **Yanıtta kod kopyala** | AI cevabındaki kod bloklarında “Kopyala” butonu. | Yüksek |
| **Pushla/PR onay özeti** | “Pushla” tıklanınca kısa modal: “Şu X dosya main’e pushlanacak. Emin misin?” (opsiyonel). | Orta |
| **Klavye kısayolları** | Örn. Ctrl+Enter gönder, Esc input’u temizle. Kısayolları ilk açılışta veya ayarlarda göstermek. | Orta |
| **Konuşma sil / temizle** | “Bu konuşmayı sil” veya “Yeni sohbet” ile geçmişi temizleme (sadece UI; isteğe bağlı backend’de soft delete). | Orta |
| **Konuşma export** | “Konuşmayı indir” (TXT/PDF) — destek talebi veya dokümantasyon için. | Düşük |
| **Boş ekran ipuçları** | İlk açılışta 2–3 kısa örnek cümle (tıklanınca input’a yazılsın). | Düşük |

---

## 3. Teknoloji güçlendirme

| Öneri | Açıklama | Öncelik |
|-------|----------|---------|
| **Proje yapısı cache** | `GetProjectStructureAsync` çıktısını kısa süreli cache’le (örn. 5 dk). Sayfa yenilemelerinde hız. | Yüksek |
| **Vektör arama kalitesi** | RAG reindex sıklığı, chunk boyutu, embedding modeli dokümante edilsin; gerekirse ince ayar. | Orta |
| **Model seçimi (UI)** | Ayarlar veya AI sayfasında “Hızlı (gpt-4o-mini) / Gelişmiş (gpt-4o)” gibi seçenek (config’e yansıyabilir). | Orta |
| **Rate limiting** | Kullanıcı / IP bazlı dakikalık istek limiti; aşımda nazik mesaj. Kötüye kullanımı azaltır. | Yüksek |
| **Structured logging** | AI istekleri, tool çağrıları, süre, hata için yapısal log (ör. Serilog + correlation id). Debug ve izleme. | Orta |

---

## 4. Ürün genişletme (yeni özellikler)

| Öneri | Açıklama | Öncelik |
|-------|----------|---------|
| **Hızlı aksiyonlar (prompt şablonları)** | “Yeni CRUD sayfası ekle”, “Bu controller’a log ekle”, “Bu sayfayı özetle” gibi tek tıkla prompt dolduran butonlar. | Yüksek |
| **Yetki: Kim push/PR yapabilir** | Sadece Admin veya belirli rol “Pushla” / “PR oluştur” kullanabilsin; diğerleri sadece soru-cevap. | Yüksek |
| **Branch adı özelleştirme** | “PR aç” derken kullanıcı branch adı önerebilsin (örn. `feature/TICKET-123-add-login`). | Düşük |
| **Build + test** | Push öncesi sadece `dotnet build` değil, isteğe bağlı `dotnet test` de çalışsın; test fail ise push yapma. | Orta |
| **CI/CD entegrasyonu** | PR açıldığında mevcut pipeline (GitHub Actions vb.) otomatik çalışsın; durum özeti (opsiyonel) AI’da gösterilebilir. | Düşük |

---

## 5. Mevcut akışı bozmadan hızlı kazanımlar

- **Sistem promptunda “yanıt süresi”**  
  Uzun işlemlerde “Build alıyorum, biraz sürebilir…” gibi ara mesajlar (tool sonucu olarak) kullanıcıya gösterilebilir.
- **Push/PR bar’ı sadece son mesajda**  
  Zaten “son mesaj push/PR sorusu mu?” ile gösteriyorsun; aynı mantık korunup diğer senaryolarda bar’ın çıkmaması sağlanabilir.
- **ConversationId + push durumu**  
  Aynı konuşmada bir kez push/PR yapıldıysa, tekrar “Pushla” gelince “Bu konuşma için zaten push yapıldı” benzeri mesaj (backend’de pending temizlendiği için zaten hata dönebilir; mesajı Türkçe ve net yap).

---

## Önerilen uygulama sırası (ilk 2–4 hafta)

1. **Audit log (AI aksiyonları)** — kim ne zaman push/PR yaptı.
2. **AI health check** — dashboard veya ayrı endpoint.
3. **Kod bloklarında “Kopyala” butonu** — basit, etkisi büyük.
4. **Rate limiting** — dakikada N istek.
5. **Retry + timeout** — OpenAI ve gerekiyorsa build/git çağrıları.
6. **Hızlı aksiyonlar** — 3–5 hazır prompt butonu.

Bu liste, ürünü genişletmek, kullanımı kolaylaştırmak, stabil ve teknolojik olarak güçlendirmek için somut adımlar sunar. İstersen bir maddeyi seçip doğrudan teknik tasarım veya kod tarafına inebiliriz.
