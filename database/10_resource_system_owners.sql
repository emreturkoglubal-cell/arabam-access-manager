-- =============================================================================
-- Uygulama (resource_systems) bazında birden fazla sorumlu kişi (owner) atanabilmesi
-- Eski tek owner_id kaldırılıp N-N tablosu (resource_system_owners) kullanılıyor.
-- =============================================================================

-- Yeni tablo: bir uygulamaya birden fazla personel sorumlu atanabilir
CREATE TABLE resource_system_owners (
    resource_system_id INT NOT NULL REFERENCES resource_systems (id) ON DELETE CASCADE,
    personnel_id        INT NOT NULL REFERENCES personnel (id) ON DELETE CASCADE,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (resource_system_id, personnel_id)
);

CREATE INDEX ix_resource_system_owners_personnel_id ON resource_system_owners (personnel_id);
CREATE INDEX ix_resource_system_owners_resource_system_id ON resource_system_owners (resource_system_id);

COMMENT ON TABLE resource_system_owners IS 'Uygulama sorumluları (bir uygulamaya birden fazla sorumlu kişi atanabilir)';

-- Mevcut owner_id verisini yeni tabloya taşı
INSERT INTO resource_system_owners (resource_system_id, personnel_id)
SELECT id, owner_id FROM resource_systems WHERE owner_id IS NOT NULL;

-- Eski tek sorumlu kolonunu kaldır
ALTER TABLE resource_systems DROP COLUMN IF EXISTS owner_id;

-- Eski index artık yok (owner_id kaldırıldı)
-- ix_resource_systems_owner_id otomatik silinmiş olur
