-- =============================================================================
-- resource_systems tablosuna Sorumlu Departman (responsible_department_id) ekler
-- =============================================================================

ALTER TABLE resource_systems
    ADD COLUMN IF NOT EXISTS responsible_department_id INT REFERENCES departments (id);

CREATE INDEX IF NOT EXISTS ix_resource_systems_responsible_department_id
    ON resource_systems (responsible_department_id) WHERE responsible_department_id IS NOT NULL;

COMMENT ON COLUMN resource_systems.responsible_department_id IS 'Sorumlu departman';
