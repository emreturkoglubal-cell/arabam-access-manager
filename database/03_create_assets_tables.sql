-- =============================================================================
-- Access Manager - Donanım & Zimmet tabloları (mevcut DB'ye ek için)
-- Ön koşul: departments, roles, personnel tabloları mevcut olmalı.
-- Bu script yoksa assets / asset_assignments / asset_assignment_notes oluşturur.
-- Tam kurulum için 00_drop_tables.sql + 01_create_tables.sql + 02_seed_data.sql
-- kullanın; sadece bu tablalar eksikse bu dosyayı çalıştırın.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. assets
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS assets (
    id              SERIAL PRIMARY KEY,
    asset_type      SMALLINT NOT NULL DEFAULT 0,
    name            VARCHAR(200) NOT NULL,
    serial_number   VARCHAR(100),
    brand_model     VARCHAR(200),
    status          SMALLINT NOT NULL DEFAULT 0,
    notes           TEXT,
    purchase_date   DATE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_assets_asset_type CHECK (asset_type BETWEEN 0 AND 7),
    CONSTRAINT chk_assets_status CHECK (status IN (0, 1, 2, 3))
);

CREATE INDEX IF NOT EXISTS ix_assets_status ON assets (status);
CREATE INDEX IF NOT EXISTS ix_assets_asset_type ON assets (asset_type);
CREATE INDEX IF NOT EXISTS ix_assets_serial_number ON assets (serial_number) WHERE serial_number IS NOT NULL;

COMMENT ON TABLE assets IS 'Donanım / varlık envanteri';
COMMENT ON COLUMN assets.asset_type IS '0=Laptop, 1=Desktop, 2=Monitor, 3=Phone, 4=Tablet, 5=Keyboard, 6=Mouse, 7=Other';
COMMENT ON COLUMN assets.status IS '0=Available, 1=Assigned, 2=InRepair, 3=Retired';

-- -----------------------------------------------------------------------------
-- 2. asset_assignments
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asset_assignments (
    id                  SERIAL PRIMARY KEY,
    asset_id            INT NOT NULL REFERENCES assets (id) ON DELETE CASCADE,
    personnel_id        INT NOT NULL REFERENCES personnel (id) ON DELETE CASCADE,
    assigned_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    assigned_by_user_id INT,
    assigned_by_user_name VARCHAR(200),
    returned_at         TIMESTAMPTZ,
    return_condition    TEXT,
    notes               TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_asset_assignments_asset_id ON asset_assignments (asset_id);
CREATE INDEX IF NOT EXISTS ix_asset_assignments_personnel_id ON asset_assignments (personnel_id);
CREATE INDEX IF NOT EXISTS ix_asset_assignments_returned_at ON asset_assignments (returned_at) WHERE returned_at IS NULL;

COMMENT ON TABLE asset_assignments IS 'Zimmet kayıtları';

-- -----------------------------------------------------------------------------
-- 3. asset_assignment_notes
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asset_assignment_notes (
    id                  SERIAL PRIMARY KEY,
    asset_assignment_id INT NOT NULL REFERENCES asset_assignments (id) ON DELETE CASCADE,
    content             TEXT NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_user_id  INT,
    created_by_user_name VARCHAR(200)
);

CREATE INDEX IF NOT EXISTS ix_asset_assignment_notes_asset_assignment_id ON asset_assignment_notes (asset_assignment_id);
CREATE INDEX IF NOT EXISTS ix_asset_assignment_notes_created_at ON asset_assignment_notes (created_at);

COMMENT ON TABLE asset_assignment_notes IS 'Zimmet notları (birden fazla, kim yazdığı takip edilir)';
