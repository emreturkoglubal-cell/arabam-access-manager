-- Uygulama (resource_systems) birim maliyeti ve para birimi: TL, USD, EUR.

ALTER TABLE resource_systems
    ADD COLUMN IF NOT EXISTS unit_cost NUMERIC(18, 2),
    ADD COLUMN IF NOT EXISTS unit_cost_currency VARCHAR(3) DEFAULT 'TRY';

UPDATE resource_systems SET unit_cost_currency = 'TRY' WHERE unit_cost_currency IS NULL;

COMMENT ON COLUMN resource_systems.unit_cost IS 'Birim maliyet (kişi başı)';
COMMENT ON COLUMN resource_systems.unit_cost_currency IS 'Birim maliyet para birimi: TRY, USD, EUR';
