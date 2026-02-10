-- =============================================================================
-- Access Manager - Tüm tabloları sil (FK sırasına göre, child önce)
-- Dikkat: Tüm veri silinir. Yedek alın.
-- =============================================================================

DROP TABLE IF EXISTS asset_assignment_notes CASCADE;
DROP TABLE IF EXISTS personnel_notes CASCADE;
DROP TABLE IF EXISTS approval_steps CASCADE;
DROP TABLE IF EXISTS personnel_accesses CASCADE;
DROP TABLE IF EXISTS role_permissions CASCADE;
DROP TABLE IF EXISTS asset_assignments CASCADE;
DROP TABLE IF EXISTS access_requests CASCADE;
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS app_users CASCADE;
DROP TABLE IF EXISTS resource_systems CASCADE;
DROP TABLE IF EXISTS personnel CASCADE;
DROP TABLE IF EXISTS assets CASCADE;
DROP TABLE IF EXISTS roles CASCADE;
DROP TABLE IF EXISTS departments CASCADE;
