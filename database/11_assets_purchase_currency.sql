-- Satın alma ücreti para birimi: TRY (TL), USD, EUR seçilebilir.

ALTER TABLE assets
    ADD COLUMN IF NOT EXISTS purchase_currency VARCHAR(3) DEFAULT 'TRY';

-- Mevcut kayıtlar için varsayılan TRY (TL)
UPDATE assets SET purchase_currency = 'TRY' WHERE purchase_currency IS NULL;

COMMENT ON COLUMN assets.purchase_currency IS 'Satın alma ücreti para birimi: TRY=TL, USD=Dolar, EUR=Euro';
