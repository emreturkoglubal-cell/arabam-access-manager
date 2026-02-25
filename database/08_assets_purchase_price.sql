-- Donanım (assets) tablosuna satın alınma ücreti alanı (TL).
-- Para birimi: TL (Türk Lirası); ondalık 2 hane.

ALTER TABLE assets
    ADD COLUMN IF NOT EXISTS purchase_price NUMERIC(18, 2);

COMMENT ON COLUMN assets.purchase_price IS 'Satın alınma ücreti (TL)';
