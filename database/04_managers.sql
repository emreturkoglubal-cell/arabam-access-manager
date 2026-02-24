-- =============================================================================
-- Managers tablosu (mevcut veritabanına ek için)
-- 01_create_tables.sql içinde zaten varsa bu script'i çalıştırmayın.
-- =============================================================================

-- Tablo yoksa oluştur
CREATE TABLE IF NOT EXISTS managers (
    id                  SERIAL PRIMARY KEY,
    personnel_id        INT NOT NULL REFERENCES personnel (id) ON DELETE CASCADE,
    level               SMALLINT NOT NULL,
    parent_manager_id   INT REFERENCES managers (id) ON DELETE SET NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_managers_personnel_id UNIQUE (personnel_id),
    CONSTRAINT chk_managers_level CHECK (level >= 1 AND level <= 4)
);

CREATE INDEX IF NOT EXISTS ix_managers_personnel_id ON managers (personnel_id);
CREATE INDEX IF NOT EXISTS ix_managers_parent_manager_id ON managers (parent_manager_id) WHERE parent_manager_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_managers_level ON managers (level);

COMMENT ON TABLE managers IS 'Yönetici hiyerarşisi: level 1 en üst, 4 en alt; personel formunda sadece en alt yönetici (leaf) listelenir';
COMMENT ON COLUMN managers.level IS '1=En üst yönetici, 4=En alt yönetici';
