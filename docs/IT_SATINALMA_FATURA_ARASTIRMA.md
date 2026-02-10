# IT Satın Alma ve Faturalandırma — Sektör Araştırması ve Öneri

## Neden gerekli?

Donanım alındığında işlemleri **bilişim departmanı** yürütüyor; faturalandırma, tedarikçi takibi ve bütçe kontrolü de bu sürecin parçası. Sipariş–fatura–varlık bağının tek yerde tutulması hem denetim hem de günlük iş takibi için faydalı.

---

## Sektörde nasıl kullanılıyor?

### 1. Satın alma yaşam döngüsü (Procure-to-Pay)

- **Talep / İstek:** Kim, ne istiyor, neden, tahmini maliyet.
- **Onay:** Maliyet/departman/bütçe eşiğine göre tek veya çok aşamalı onay (yönetici, finans, IT).
- **Sipariş (PO):** Onay sonrası sipariş numarası, tedarikçi, kalemler (ürün, miktar, birim fiyat).
- **Teslim alma:** Mal gelince siparişle eşleştirme; “Teslim alındı” ile varlık (asset) kaydı oluşturma.
- **Fatura:** Tedarikçi faturası; PO ve teslim kaydı ile eşleştirme (2/3-way match).
- **Ödeme:** Fatura onayı, ödeme tarihi, durum (Beklemede / Ödendi).

### 2. Sipariş (PO) ve fatura alanları

**Sipariş (PO) tarafında tipik alanlar:**

- PO numarası (benzersiz)
- Tedarikçi (vendor)
- Talep eden / onaylayan (personel)
- Maliyet merkezi / departman (bütçe ataması)
- Durum: Taslak, Onay bekliyor, Onaylandı, Sipariş verildi, Kısmen teslim alındı, Teslim alındı, İptal
- Sipariş tarihi, teslim tarihi
- Kalemler: Açıklama (veya donanım türü), miktar, birim fiyat, toplam

**Fatura tarafında tipik alanlar:**

- Fatura numarası (tedarikçi numarası)
- Fatura tarihi
- Tedarikçi
- İlişkili PO (bir fatura bir veya birden fazla PO’ya bağlanabilir)
- Tutar, vergi, toplam
- Durum: Beklemede, Onaylandı, Ödendi
- Ödeme tarihi (opsiyonel)

### 3. Sektör vurguları

- **PO numarası:** Sipariş–fatura–varlık arasında ana bağ; donanım kaydında PO referansı tutulur.
- **Onay akışı:** Maliyet eşiğine göre farklı onay seviyeleri (örn. belirli tutar üstü finans onayı).
- **Üçlü eşleştirme (three-way match):** PO ↔ Teslim alma ↔ Fatura uyumu, hata ve dolandırıcılığı azaltır.
- **Maliyet merkezi / bütçe:** Harcamanın hangi departman/bütçeye yazılacağı; raporlama ve bütçe takibi için gerekli.
- **Denetim izi:** Talep → onay → sipariş → fatura → ödeme adımlarının loglanması.

---

## Mevcut sistemle uyum

AccessManager’da zaten var:

- **Personel, departman** → Talep eden, onaylayan, maliyet merkezi.
- **Donanım (Asset), zimmet (AssetAssignment)** → Teslim alındığında oluşturulacak varlık kaydı.

Eklenecek mantıksal bloklar:

1. **Tedarikçi (Vendor):** Firma adı, iletişim, vergi no vb. (basit liste).
2. **Satın alma siparişi (Purchase Order):** Talep + onay + sipariş + teslim durumu; isteğe bağlı kalem detayı.
3. **Fatura (Invoice):** Fatura no, tarih, tedarikçi, tutar, PO ilişkisi, ödeme durumu.

İsteğe bağlı ileride: bütçe limiti, maliyet eşiğine göre otomatik onay yönlendirmesi.

---

## Önerilen kapsam (MVP)

### Aşama 1 — Temel modeller ve sayfalar

| Bileşen | İçerik |
|--------|--------|
| **Tedarikçiler** | Liste, ekleme, düzenleme. Ad, kodu, iletişim (e-posta, telefon), vergi numarası (opsiyonel). |
| **Satın alma siparişleri (PO)** | Liste (filtre: durum, tedarikçi, departman), detay, yeni sipariş. Alanlar: PO no, tedarikçi, talep eden (personel), departman (maliyet merkezi), sipariş tarihi, durum, toplam tutar; kalemler: açıklama/donanım türü, miktar, birim fiyat. Onay: onaylayan, onay tarihi (tek aşama yeterli başlangıç için). |
| **Faturalar** | Liste (filtre: tedarikçi, durum), detay, yeni fatura. Alanlar: Fatura no, fatura tarihi, tedarikçi, ilişkili PO, tutar, durum (Beklemede / Ödendi), ödeme tarihi. |
| **Donanım bağlantısı** | PO “Teslim alındı” yapıldığında ilgili donanım kayıtlarının (Asset) oluşturulması veya mevcut donanıma PO numarası atanması. |

### Aşama 2 (ileride)

- Maliyet eşiğine göre çok aşamalı onay.
- Departman bazlı basit bütçe / harcama özeti.
- Fatura ↔ PO eşleştirme uyarıları (tutar, kalem farkı).

---

## Kullanıcı akışı (özet)

1. IT veya yönetici **satın alma siparişi** açar: tedarikçi, kalemler, maliyet merkezi (departman).
2. Onay süreci işler (şimdilik tek onay yeterli).
3. Sipariş **“Sipariş verildi”** / **“Teslim alındı”** güncellenir.
4. **Fatura** girilir; PO’ya bağlanır; tutar ve ödeme durumu işlenir.
5. Teslim alındığında donanım envanterine **Asset** eklenir (veya mevcut asset’e PO referansı verilir).

Bu sayede donanım, sipariş ve fatura tek yerden takip edilir; denetim ve bütçe konuşmaları kolaylaşır.

---

## Sonuç

- Sektörde **sipariş (PO) + fatura + varlık** birlikte yönetiliyor; PO numarası ve maliyet merkezi ortak ihtiyaç.
- MVP için **Tedarikçi**, **Satın alma siparişi (PO)** ve **Fatura** sayfaları ile PO–Asset bağını eklemek, bilişim ve fatura süreçlerini tek sistemde toplamak için sağlam bir başlangıç olur.
- İstersen bir sonraki adımda domain entity’leri (Vendor, PurchaseOrder, PurchaseOrderLine, Invoice) ve basit liste/detay/oluşturma ekranlarını tek tek çıkarabiliriz.
