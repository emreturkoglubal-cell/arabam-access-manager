-- =============================================================================
-- managers tablosuna is_active kolonu (pasife alınan yöneticiler dropdown'da çıkmaz)
-- =============================================================================

ALTER TABLE managers
    ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT true;

CREATE INDEX IF NOT EXISTS ix_managers_is_active ON managers (is_active) WHERE is_active = true;

COMMENT ON COLUMN managers.is_active IS 'false ise yönetici pasif; personel formu dropdown''ında listelenmez';
