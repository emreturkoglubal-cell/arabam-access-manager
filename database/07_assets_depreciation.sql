-- Donanım (assets) tablosuna amortisman tarihi alanı.
-- Varsayılan mantık: satın alma tarihi + 5 yıl (uygulama tarafında hesaplanır / varsayılan doldurulur).
-- Mevcut satın alma tarihi olan kayıtlar için amortisman tarihi = purchase_date + 5 yıl ile güncellenir.

ALTER TABLE assets
    ADD COLUMN IF NOT EXISTS depreciation_end_date DATE;

COMMENT ON COLUMN assets.depreciation_end_date IS 'Amortisman bitiş tarihi; boşsa satın alma tarihi + 5 yıl kabul edilir';

-- İsteğe bağlı: mevcut kayıtlarda purchase_date dolu ama depreciation_end_date boş olanları doldur (varsayılan 5 yıl)
UPDATE assets
SET depreciation_end_date = purchase_date + INTERVAL '5 years'
WHERE purchase_date IS NOT NULL
  AND depreciation_end_date IS NULL;
