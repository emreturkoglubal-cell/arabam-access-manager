-- =============================================================================
-- Access Manager - PostgreSQL DDL (INT id, SERIAL otomatik artan)
-- Tablo isimleri: çoğul, küçük harf, kelimeler arasında alt tire
-- Sütun isimleri: snake_case
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. departments
-- -----------------------------------------------------------------------------
CREATE TABLE departments (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    code            VARCHAR(50),
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_departments_code ON departments (code) WHERE code IS NOT NULL;
CREATE INDEX ix_departments_name ON departments (name);

COMMENT ON TABLE departments IS 'Organizasyon departmanları';

-- -----------------------------------------------------------------------------
-- 2. roles
-- -----------------------------------------------------------------------------
CREATE TABLE roles (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    code            VARCHAR(50),
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_roles_code ON roles (code) WHERE code IS NOT NULL;
CREATE INDEX ix_roles_name ON roles (name);

COMMENT ON TABLE roles IS 'İş rolleri (varsayılan yetkiler atanır)';

-- -----------------------------------------------------------------------------
-- 3. personnel
-- -----------------------------------------------------------------------------
CREATE TABLE personnel (
    id              SERIAL PRIMARY KEY,
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    email           VARCHAR(255) NOT NULL,
    department_id   INT NOT NULL REFERENCES departments (id),
    position        VARCHAR(200),
    manager_id      INT REFERENCES personnel (id),
    start_date      DATE NOT NULL,
    end_date        DATE,
    status          SMALLINT NOT NULL DEFAULT 0,
    role_id         INT REFERENCES roles (id),
    location        VARCHAR(200),
    image_url       VARCHAR(500),
    rating          DECIMAL(3,1),
    manager_comment TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_personnel_status CHECK (status IN (0, 1, 2))
);

CREATE INDEX ix_personnel_department_id ON personnel (department_id);
CREATE INDEX ix_personnel_manager_id ON personnel (manager_id) WHERE manager_id IS NOT NULL;
CREATE INDEX ix_personnel_role_id ON personnel (role_id) WHERE role_id IS NOT NULL;
CREATE INDEX ix_personnel_status ON personnel (status);
CREATE INDEX ix_personnel_email ON personnel (email);
CREATE INDEX ix_personnel_end_date ON personnel (end_date) WHERE end_date IS NOT NULL;

COMMENT ON TABLE personnel IS 'Personel kayıtları';
COMMENT ON COLUMN personnel.status IS '0=Active, 1=Passive, 2=Offboarded';

-- -----------------------------------------------------------------------------
-- 4. resource_systems
-- -----------------------------------------------------------------------------
CREATE TABLE resource_systems (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    code            VARCHAR(50),
    system_type     SMALLINT NOT NULL DEFAULT 0,
    critical_level  SMALLINT NOT NULL DEFAULT 1,
    owner_id        INT REFERENCES personnel (id),
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_resource_systems_system_type CHECK (system_type IN (0, 1, 2)),
    CONSTRAINT chk_resource_systems_critical_level CHECK (critical_level IN (0, 1, 2))
);

CREATE INDEX ix_resource_systems_owner_id ON resource_systems (owner_id) WHERE owner_id IS NOT NULL;
CREATE INDEX ix_resource_systems_code ON resource_systems (code) WHERE code IS NOT NULL;
CREATE INDEX ix_resource_systems_system_type ON resource_systems (system_type);

COMMENT ON TABLE resource_systems IS 'Uygulama / sistem envanteri';
COMMENT ON COLUMN resource_systems.system_type IS '0=Application, 1=Infrastructure, 2=License';
COMMENT ON COLUMN resource_systems.critical_level IS '0=Low, 1=Medium, 2=High';

-- -----------------------------------------------------------------------------
-- 5. app_users
-- -----------------------------------------------------------------------------
CREATE TABLE app_users (
    id              SERIAL PRIMARY KEY,
    user_name       VARCHAR(100) NOT NULL,
    display_name    VARCHAR(200) NOT NULL,
    password_hash   VARCHAR(500) NOT NULL,
    role            SMALLINT NOT NULL DEFAULT 2,
    personnel_id    INT REFERENCES personnel (id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_app_users_user_name UNIQUE (user_name),
    CONSTRAINT chk_app_users_role CHECK (role BETWEEN 0 AND 4)
);

CREATE INDEX ix_app_users_personnel_id ON app_users (personnel_id) WHERE personnel_id IS NOT NULL;
CREATE INDEX ix_app_users_role ON app_users (role);

COMMENT ON TABLE app_users IS 'Uygulama giriş kullanıcıları (kimlik doğrulama)';
COMMENT ON COLUMN app_users.role IS '0=Admin, 1=Manager, 2=User, 3=Auditor, 4=Viewer';

-- -----------------------------------------------------------------------------
-- 6. role_permissions
-- -----------------------------------------------------------------------------
CREATE TABLE role_permissions (
    id                  SERIAL PRIMARY KEY,
    role_id             INT NOT NULL REFERENCES roles (id) ON DELETE CASCADE,
    resource_system_id  INT NOT NULL REFERENCES resource_systems (id) ON DELETE CASCADE,
    permission_type     SMALLINT NOT NULL DEFAULT 0,
    is_default          BOOLEAN NOT NULL DEFAULT true,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_role_permissions_role_system_permission UNIQUE (role_id, resource_system_id, permission_type),
    CONSTRAINT chk_role_permissions_permission_type CHECK (permission_type BETWEEN 0 AND 5)
);

CREATE INDEX ix_role_permissions_role_id ON role_permissions (role_id);
CREATE INDEX ix_role_permissions_resource_system_id ON role_permissions (resource_system_id);

COMMENT ON TABLE role_permissions IS 'Rol bazlı varsayılan yetkiler';
COMMENT ON COLUMN role_permissions.permission_type IS '0=Read, 1=Write, 2=Admin, 3=Custom, 4=Open, 5=Closed';

-- -----------------------------------------------------------------------------
-- 7. access_requests
-- -----------------------------------------------------------------------------
CREATE TABLE access_requests (
    id                  SERIAL PRIMARY KEY,
    personnel_id        INT NOT NULL REFERENCES personnel (id),
    resource_system_id  INT NOT NULL REFERENCES resource_systems (id),
    requested_permission SMALLINT NOT NULL DEFAULT 0,
    reason              TEXT,
    start_date          DATE,
    end_date            DATE,
    status              SMALLINT NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          INT NOT NULL REFERENCES personnel (id),
    CONSTRAINT chk_access_requests_status CHECK (status BETWEEN 0 AND 7),
    CONSTRAINT chk_access_requests_permission CHECK (requested_permission BETWEEN 0 AND 5)
);

CREATE INDEX ix_access_requests_personnel_id ON access_requests (personnel_id);
CREATE INDEX ix_access_requests_resource_system_id ON access_requests (resource_system_id);
CREATE INDEX ix_access_requests_status ON access_requests (status);
CREATE INDEX ix_access_requests_created_at ON access_requests (created_at);

COMMENT ON TABLE access_requests IS 'Yetki talepleri';
COMMENT ON COLUMN access_requests.status IS '0=Draft, 1=PendingManager, 2=PendingSystemOwner, 3=PendingIT, 4=Approved, 5=Rejected, 6=Applied, 7=Expired';

-- -----------------------------------------------------------------------------
-- 8. approval_steps
-- -----------------------------------------------------------------------------
CREATE TABLE approval_steps (
    id                  SERIAL PRIMARY KEY,
    access_request_id   INT NOT NULL REFERENCES access_requests (id) ON DELETE CASCADE,
    step_name           VARCHAR(100) NOT NULL,
    approved_by         INT REFERENCES personnel (id),
    approved_by_name    VARCHAR(200),
    approved_at         TIMESTAMPTZ,
    approved            BOOLEAN,
    comment             TEXT,
    "order"             INT NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_approval_steps_access_request_id ON approval_steps (access_request_id);
CREATE INDEX ix_approval_steps_approved_by ON approval_steps (approved_by) WHERE approved_by IS NOT NULL;

COMMENT ON TABLE approval_steps IS 'Yetki talebi onay adımları';

-- -----------------------------------------------------------------------------
-- 9. personnel_accesses
-- -----------------------------------------------------------------------------
CREATE TABLE personnel_accesses (
    id                      SERIAL PRIMARY KEY,
    personnel_id            INT NOT NULL REFERENCES personnel (id) ON DELETE CASCADE,
    resource_system_id      INT NOT NULL REFERENCES resource_systems (id) ON DELETE CASCADE,
    permission_type         SMALLINT NOT NULL DEFAULT 0,
    is_exception            BOOLEAN NOT NULL DEFAULT false,
    granted_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at              TIMESTAMPTZ,
    is_active               BOOLEAN NOT NULL DEFAULT true,
    granted_by_request_id   INT REFERENCES access_requests (id),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_personnel_accesses_permission_type CHECK (permission_type BETWEEN 0 AND 5)
);

CREATE INDEX ix_personnel_accesses_personnel_id ON personnel_accesses (personnel_id);
CREATE INDEX ix_personnel_accesses_resource_system_id ON personnel_accesses (resource_system_id);
CREATE INDEX ix_personnel_accesses_is_active ON personnel_accesses (is_active);
CREATE INDEX ix_personnel_accesses_expires_at ON personnel_accesses (expires_at) WHERE expires_at IS NOT NULL;
CREATE INDEX ix_personnel_accesses_personnel_active ON personnel_accesses (personnel_id, is_active) WHERE is_active = true;

COMMENT ON TABLE personnel_accesses IS 'Personel yetki kayıtları (açık/kapatıldı)';

-- -----------------------------------------------------------------------------
-- 10. audit_logs
-- -----------------------------------------------------------------------------
CREATE TABLE audit_logs (
    id          SERIAL PRIMARY KEY,
    actor_id    INT,
    actor_name  VARCHAR(200) NOT NULL,
    action      SMALLINT NOT NULL,
    target_type VARCHAR(100) NOT NULL,
    target_id   VARCHAR(100),
    details     TEXT,
    timestamp   TIMESTAMPTZ NOT NULL DEFAULT now(),
    ip_address  VARCHAR(45),
    CONSTRAINT chk_audit_logs_action CHECK (action BETWEEN 0 AND 30)
);

CREATE INDEX ix_audit_logs_timestamp ON audit_logs (timestamp);
CREATE INDEX ix_audit_logs_target_type ON audit_logs (target_type);
CREATE INDEX ix_audit_logs_actor_id ON audit_logs (actor_id) WHERE actor_id IS NOT NULL;

COMMENT ON TABLE audit_logs IS 'Denetim / log kayıtları';

-- -----------------------------------------------------------------------------
-- 11. assets
-- -----------------------------------------------------------------------------
CREATE TABLE assets (
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

CREATE INDEX ix_assets_status ON assets (status);
CREATE INDEX ix_assets_asset_type ON assets (asset_type);
CREATE INDEX ix_assets_serial_number ON assets (serial_number) WHERE serial_number IS NOT NULL;

COMMENT ON TABLE assets IS 'Donanım / varlık envanteri';
COMMENT ON COLUMN assets.asset_type IS '0=Laptop, 1=Desktop, 2=Monitor, 3=Phone, 4=Tablet, 5=Keyboard, 6=Mouse, 7=Other';
COMMENT ON COLUMN assets.status IS '0=Available, 1=Assigned, 2=InRepair, 3=Retired';

-- -----------------------------------------------------------------------------
-- 12. asset_assignments
-- -----------------------------------------------------------------------------
CREATE TABLE asset_assignments (
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

CREATE INDEX ix_asset_assignments_asset_id ON asset_assignments (asset_id);
CREATE INDEX ix_asset_assignments_personnel_id ON asset_assignments (personnel_id);
CREATE INDEX ix_asset_assignments_returned_at ON asset_assignments (returned_at) WHERE returned_at IS NULL;

COMMENT ON TABLE asset_assignments IS 'Zimmet kayıtları';

-- -----------------------------------------------------------------------------
-- 13. asset_assignment_notes
-- -----------------------------------------------------------------------------
CREATE TABLE asset_assignment_notes (
    id                  SERIAL PRIMARY KEY,
    asset_assignment_id INT NOT NULL REFERENCES asset_assignments (id) ON DELETE CASCADE,
    content             TEXT NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_user_id  INT,
    created_by_user_name VARCHAR(200)
);

CREATE INDEX ix_asset_assignment_notes_asset_assignment_id ON asset_assignment_notes (asset_assignment_id);
CREATE INDEX ix_asset_assignment_notes_created_at ON asset_assignment_notes (created_at);

COMMENT ON TABLE asset_assignment_notes IS 'Zimmet notları (birden fazla, kim yazdığı takip edilir)';

-- -----------------------------------------------------------------------------
-- 14. personnel_notes
-- -----------------------------------------------------------------------------
CREATE TABLE personnel_notes (
    id                  SERIAL PRIMARY KEY,
    personnel_id        INT NOT NULL REFERENCES personnel (id) ON DELETE CASCADE,
    content             TEXT NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_user_id  INT,
    created_by_user_name VARCHAR(200)
);

CREATE INDEX ix_personnel_notes_personnel_id ON personnel_notes (personnel_id);
CREATE INDEX ix_personnel_notes_created_at ON personnel_notes (created_at);

COMMENT ON TABLE personnel_notes IS 'Personel notları (birden fazla, kim yazdığı takip edilir)';

-- -----------------------------------------------------------------------------
-- 15. revise_requests
-- -----------------------------------------------------------------------------
CREATE TABLE revise_requests (
    id                  SERIAL PRIMARY KEY,
    title               VARCHAR(500) NOT NULL,
    description         TEXT NOT NULL,
    status              SMALLINT NOT NULL DEFAULT 0,
    created_by_user_id INT REFERENCES app_users (id),
    created_by_user_name VARCHAR(200),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    resolved_at         TIMESTAMPTZ,
    CONSTRAINT chk_revise_requests_status CHECK (status IN (0, 1))
);

CREATE INDEX ix_revise_requests_status ON revise_requests (status);
CREATE INDEX ix_revise_requests_created_at ON revise_requests (created_at DESC);
CREATE INDEX ix_revise_requests_created_by_user_id ON revise_requests (created_by_user_id) WHERE created_by_user_id IS NOT NULL;

COMMENT ON TABLE revise_requests IS 'Geliştiriciden talepler (bug bildirimi, özellik istekleri)';
COMMENT ON COLUMN revise_requests.status IS '0=Çözülmedi, 1=Çözüldü';

-- -----------------------------------------------------------------------------
-- 16. revise_request_images
-- -----------------------------------------------------------------------------
CREATE TABLE revise_request_images (
    id                  SERIAL PRIMARY KEY,
    revise_request_id   INT NOT NULL REFERENCES revise_requests (id) ON DELETE CASCADE,
    file_name           VARCHAR(500) NOT NULL,
    file_path           VARCHAR(1000) NOT NULL,
    file_size           BIGINT NOT NULL,
    mime_type           VARCHAR(100),
    display_order       INT NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_revise_request_images_revise_request_id ON revise_request_images (revise_request_id);
CREATE INDEX ix_revise_request_images_display_order ON revise_request_images (revise_request_id, display_order);

COMMENT ON TABLE revise_request_images IS 'Geliştiriciden talep fotoğrafları (birden fazla)';
