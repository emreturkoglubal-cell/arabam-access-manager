-- =============================================================================
-- Access Manager - Yönetici istekleri için şema güncellemeleri
-- Çalıştırma sırası: 15 (01-14 sonrası)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 15.1 asset_assignments: iadeyi alan kişi (returned_by)
-- -----------------------------------------------------------------------------
ALTER TABLE asset_assignments
    ADD COLUMN IF NOT EXISTS returned_by_user_id INT,
    ADD COLUMN IF NOT EXISTS returned_by_user_name VARCHAR(200);

COMMENT ON COLUMN asset_assignments.returned_by_user_id IS 'Zimmet iadesini alan kullanıcı (app_users id)';
COMMENT ON COLUMN asset_assignments.returned_by_user_name IS 'Zimmet iadesini alan kişi adı';

-- -----------------------------------------------------------------------------
-- 15.2 personnel: seviye (Jr, Mid, Sr, Lead), ekip, ünvan aynı position'da kalacak
-- -----------------------------------------------------------------------------
ALTER TABLE personnel
    ADD COLUMN IF NOT EXISTS seniority_level VARCHAR(50),
    ADD COLUMN IF NOT EXISTS team_id INT;

COMMENT ON COLUMN personnel.seniority_level IS 'Seviye: Jr, Mid, Sr, Lead vb.';
COMMENT ON COLUMN personnel.team_id IS 'Alt ekip (teams tablosuna FK)';

-- -----------------------------------------------------------------------------
-- 15.3 teams: departmana bağlı ekipler (Bilgi Teknolojileri -> DevOps, Development)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS teams (
    id              SERIAL PRIMARY KEY,
    department_id    INT NOT NULL REFERENCES departments (id) ON DELETE CASCADE,
    name            VARCHAR(200) NOT NULL,
    code            VARCHAR(50),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_teams_department_id ON teams (department_id);
CREATE INDEX IF NOT EXISTS ix_teams_name ON teams (name);

COMMENT ON TABLE teams IS 'Departman alt ekipleri (örn. IT -> DevOps, Development)';

-- personnel.team_id FK (teams sonra oluşturulduğu için burada)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_schema = 'public' AND constraint_name = 'fk_personnel_team' AND table_name = 'personnel'
    ) THEN
        ALTER TABLE personnel ADD CONSTRAINT fk_personnel_team FOREIGN KEY (team_id) REFERENCES teams (id) ON DELETE SET NULL;
    END IF;
END $$;

-- -----------------------------------------------------------------------------
-- 15.4 departments: üst departman (alt kırılım), departman yöneticisi (GMY)
-- -----------------------------------------------------------------------------
ALTER TABLE departments
    ADD COLUMN IF NOT EXISTS parent_id INT REFERENCES departments (id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS top_manager_personnel_id INT REFERENCES personnel (id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS ix_departments_parent_id ON departments (parent_id) WHERE parent_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_departments_top_manager ON departments (top_manager_personnel_id) WHERE top_manager_personnel_id IS NOT NULL;

COMMENT ON COLUMN departments.parent_id IS 'Üst departman (alt kırılım: IT -> DevOps)';
COMMENT ON COLUMN departments.top_manager_personnel_id IS 'Departman en üst yöneticisi (GMY/Direktör)';

-- -----------------------------------------------------------------------------
-- 15.5 department_managers: 1./2./3. yönetici, birden fazla kişi
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS department_managers (
    id              SERIAL PRIMARY KEY,
    department_id   INT NOT NULL REFERENCES departments (id) ON DELETE CASCADE,
    personnel_id    INT NOT NULL REFERENCES personnel (id) ON DELETE CASCADE,
    manager_level   SMALLINT NOT NULL DEFAULT 1,
    display_order   INT NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_department_managers_level CHECK (manager_level IN (1, 2, 3))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_department_managers_dept_person_level ON department_managers (department_id, personnel_id, manager_level);
CREATE INDEX IF NOT EXISTS ix_department_managers_department_id ON department_managers (department_id);
CREATE INDEX IF NOT EXISTS ix_department_managers_personnel_id ON department_managers (personnel_id);

COMMENT ON TABLE department_managers IS 'Departman 1./2./3. yöneticileri (birden fazla kişi atanabilir)';
COMMENT ON COLUMN department_managers.manager_level IS '1=1. Yönetici, 2=2. Yönetici, 3=3. Yönetici';

-- -----------------------------------------------------------------------------
-- 15.6 personnel_reminders: schedule / hatırlatma + mail
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS personnel_reminders (
    id              SERIAL PRIMARY KEY,
    personnel_id    INT NOT NULL REFERENCES personnel (id) ON DELETE CASCADE,
    reminder_date   DATE NOT NULL,
    description     TEXT NOT NULL,
    sent_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_user_id INT,
    created_by_user_name VARCHAR(200)
);

CREATE INDEX IF NOT EXISTS ix_personnel_reminders_personnel_id ON personnel_reminders (personnel_id);
CREATE INDEX IF NOT EXISTS ix_personnel_reminders_reminder_date ON personnel_reminders (reminder_date);
CREATE INDEX IF NOT EXISTS ix_personnel_reminders_sent_at ON personnel_reminders (sent_at) WHERE sent_at IS NULL;

COMMENT ON TABLE personnel_reminders IS 'Personel hatırlatmaları; reminder_date günü mail atılır';

-- -----------------------------------------------------------------------------
-- 15.7 assets: amortisman yılı (1-5), cihaz özellikleri, ek durumlar
-- -----------------------------------------------------------------------------
-- Amortisman süresi (yıl)
ALTER TABLE assets
    ADD COLUMN IF NOT EXISTS depreciation_years SMALLINT;

COMMENT ON COLUMN assets.depreciation_years IS 'Amortisman süresi (yıl); 1-5';

-- Cihaz türüne göre özellikler (Laptop: ram, storage, cpu, screen; Phone/Tablet: screen, ram, storage; Monitor: pivot, screen)
ALTER TABLE assets
    ADD COLUMN IF NOT EXISTS spec_ram_gb INT,
    ADD COLUMN IF NOT EXISTS spec_storage_gb INT,
    ADD COLUMN IF NOT EXISTS spec_cpu VARCHAR(200),
    ADD COLUMN IF NOT EXISTS spec_screen_inches DECIMAL(5,2),
    ADD COLUMN IF NOT EXISTS spec_is_pivot BOOLEAN;

COMMENT ON COLUMN assets.spec_ram_gb IS 'RAM (GB) - Laptop/Phone/Tablet';
COMMENT ON COLUMN assets.spec_storage_gb IS 'Depolama (GB) - Laptop/Phone/Tablet';
COMMENT ON COLUMN assets.spec_cpu IS 'İşlemci - Laptop';
COMMENT ON COLUMN assets.spec_screen_inches IS 'Ekran boyutu (inç)';
COMMENT ON COLUMN assets.spec_is_pivot IS 'Pivot (döndürülebilir) - Monitör';

-- Yeni durumlar: 4=Satılacak, 5=Test (mevcut: 0=Available, 1=Assigned, 2=InRepair, 3=Retired)
ALTER TABLE assets DROP CONSTRAINT IF EXISTS chk_assets_status;
ALTER TABLE assets ADD CONSTRAINT chk_assets_status CHECK (status IN (0, 1, 2, 3, 4, 5));
COMMENT ON COLUMN assets.status IS '0=Available, 1=Assigned, 2=InRepair, 3=Retired, 4=ForSale, 5=Test';

-- -----------------------------------------------------------------------------
-- 15.8 resource_system_monthly_snapshots: uygulama aylık maliyet/erişim (ileride grafik)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS resource_system_monthly_snapshots (
    id                      SERIAL PRIMARY KEY,
    resource_system_id       INT NOT NULL REFERENCES resource_systems (id) ON DELETE CASCADE,
    year_month              DATE NOT NULL,
    active_access_count     INT NOT NULL DEFAULT 0,
    total_cost_amount       NUMERIC(18, 2),
    total_cost_currency     VARCHAR(3),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_snapshot_system_month UNIQUE (resource_system_id, year_month)
);

CREATE INDEX IF NOT EXISTS ix_rs_snapshots_system ON resource_system_monthly_snapshots (resource_system_id);
CREATE INDEX IF NOT EXISTS ix_rs_snapshots_year_month ON resource_system_monthly_snapshots (year_month);

COMMENT ON TABLE resource_system_monthly_snapshots IS 'Uygulama aylık erişim/maliyet anlık görüntüsü (grafik için; job ile doldurulur)';
