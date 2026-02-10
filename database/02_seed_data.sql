-- =============================================================================
-- Access Manager - Seed data (MockDataStore ile aynı veriler, INT id)
-- Tablolar oluşturulduktan sonra çalıştırın: 01_create_tables.sql
-- Insert sırası FK'lara göre; access_requests, personnel_accesses'tan önce.
-- =============================================================================

-- id eşlemesi: departments 1-4, roles 1-5, personnel 1-5 (1=manager, 2=dev1, 3=dev2, 4=hr1, 5=ex),
-- resource_systems 1-7, access_requests 1-3 (2=ERP talebi)

-- -----------------------------------------------------------------------------
-- 1. departments
-- -----------------------------------------------------------------------------
INSERT INTO departments (id, name, code, description) VALUES
    (1, 'Bilgi İşlem', 'IT', NULL),
    (2, 'İnsan Kaynakları', 'HR', NULL),
    (3, 'Yazılım Geliştirme', 'DEV', NULL),
    (4, 'Finans', 'FIN', NULL);

-- -----------------------------------------------------------------------------
-- 2. roles
-- -----------------------------------------------------------------------------
INSERT INTO roles (id, name, code, description) VALUES
    (1, 'Backend Developer', 'BE', NULL),
    (2, 'Frontend Developer', 'FE', NULL),
    (3, 'HR Specialist', 'HR', NULL),
    (4, 'IT Admin', 'IT', NULL),
    (5, 'Finance User', 'FIN', NULL);

-- -----------------------------------------------------------------------------
-- 3. personnel (1=manager, 2=dev1, 3=dev2, 4=hr1, 5=ex_employee)
-- -----------------------------------------------------------------------------
INSERT INTO personnel (id, sicil_no, first_name, last_name, email, department_id, position, manager_id, start_date, end_date, status, role_id, location, image_url, rating, manager_comment) VALUES
    (1, '1001', 'Enes Emre', 'Arıkan', 'ahmet.yilmaz@arabam.com', 1, 'Bilgi İşlem Müdürü', NULL, '2020-01-01', NULL, 0, 4, NULL, 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400', NULL, NULL),
    (2, '1002', 'Mehmet', 'Kaya', 'mehmet.kaya@arabam.com', 3, 'Backend Geliştirici', 1, '2022-03-15', NULL, 0, 1, NULL, NULL, 8.0, 'Teknik bilgisi güçlü, projelere zamanında teslim. Takım içi iletişimi iyi.'),
    (3, '1003', 'Ayşe', 'Demir', 'ayse.demir@arabam.com', 3, 'Frontend Geliştirici', 1, '2023-06-01', NULL, 0, 2, NULL, NULL, 7.5, 'Takım çalışması çok iyi. UI/UX konusunda gelişmeye açık.'),
    (4, '1004', 'Fatma', 'Şahin', 'fatma.sahin@arabam.com', 2, 'İK Uzmanı', 1, '2021-09-01', NULL, 0, 3, NULL, NULL, 9.0, 'Düzenli ve sorumlu. İşe alım süreçlerinde başarılı.'),
    (5, '1000', 'Eski', 'Personel', 'eski@arabam.com', 3, 'Eski Geliştirici', 1, '2020-01-01', '2024-11-30', 2, 1, NULL, NULL, NULL, NULL);

-- -----------------------------------------------------------------------------
-- 4. resource_systems (1-7, owner_id = 1)
-- -----------------------------------------------------------------------------
INSERT INTO resource_systems (id, name, code, system_type, critical_level, owner_id, description) VALUES
    (1, 'Bitbucket', 'BIT', 0, 1, 1, NULL),
    (2, 'Kurumsal Mail', 'MAIL', 0, 2, 1, NULL),
    (3, 'VPN', 'VPN', 1, 2, 1, NULL),
    (4, 'Office 365', 'O365', 2, 0, 1, NULL),
    (5, 'ERP', 'ERP', 0, 2, 1, NULL),
    (6, 'Test Ortamları', 'TEST', 1, 1, 1, NULL),
    (7, 'Eski Proje (Kapatıldı)', 'LEGACY', 0, 0, 1, NULL);

-- -----------------------------------------------------------------------------
-- 5. app_users (parola demo: Password1)
-- -----------------------------------------------------------------------------
INSERT INTO app_users (id, user_name, display_name, password_hash, role, personnel_id) VALUES
    (1, 'admin', 'Sistem Yöneticisi', 'Password1', 0, NULL),
    (2, 'manager', 'Yetki Yöneticisi', 'Password1', 1, 1),
    (3, 'user', 'Standart Kullanıcı', 'Password1', 2, NULL),
    (4, 'auditor', 'Denetçi', 'Password1', 3, NULL),
    (5, 'viewer', 'İzleyici', 'Password1', 4, NULL);

-- -----------------------------------------------------------------------------
-- 6. role_permissions
-- -----------------------------------------------------------------------------
INSERT INTO role_permissions (id, role_id, resource_system_id, permission_type, is_default) VALUES
    (1, 1, 1, 0, true),
    (2, 1, 1, 1, true),
    (3, 1, 3, 0, true),
    (4, 1, 6, 0, true),
    (5, 1, 2, 0, true),
    (6, 1, 4, 0, true),
    (7, 4, 1, 2, true),
    (8, 4, 2, 2, true),
    (9, 4, 3, 2, true),
    (10, 3, 2, 0, true),
    (11, 3, 4, 0, true),
    (12, 5, 5, 0, true),
    (13, 5, 4, 0, true);

-- -----------------------------------------------------------------------------
-- 7. access_requests (ÖNCE bunlar; personnel_accesses granted_by_request_id 2'yi referans ediyor)
-- -----------------------------------------------------------------------------
INSERT INTO access_requests (id, personnel_id, resource_system_id, requested_permission, reason, start_date, end_date, status, created_at, created_by) VALUES
    (1, 3, 1, 1, 'Proje geliştirmesi', NULL, NULL, 6, (CURRENT_TIMESTAMP - INTERVAL '5 days'), 3),
    (2, 2, 5, 0, 'Raporlama', (CURRENT_DATE - INTERVAL '30 days'), (CURRENT_DATE + INTERVAL '30 days'), 6, (CURRENT_TIMESTAMP - INTERVAL '35 days'), 2),
    (3, 3, 6, 1, 'Test deployment', NULL, NULL, 1, CURRENT_TIMESTAMP, 3);

-- -----------------------------------------------------------------------------
-- 8. approval_steps
-- -----------------------------------------------------------------------------
INSERT INTO approval_steps (id, access_request_id, step_name, approved_by, approved_by_name, approved_at, approved, comment, "order") VALUES
    (1, 1, 'Manager', 1, 'Enes Emre Arıkan', (CURRENT_TIMESTAMP - INTERVAL '4 days'), true, NULL, 1),
    (2, 1, 'IT', 1, 'Enes Emre Arıkan', (CURRENT_TIMESTAMP - INTERVAL '3 days'), true, NULL, 2),
    (3, 3, 'Manager', NULL, NULL, NULL, NULL, NULL, 1);

-- -----------------------------------------------------------------------------
-- 9. personnel_accesses (access_requests artık var; granted_by_request_id = 2 kullanılıyor)
-- -----------------------------------------------------------------------------
-- Eski personel (5): Bitbucket, Mail, VPN açık
INSERT INTO personnel_accesses (personnel_id, resource_system_id, permission_type, is_exception, granted_at, expires_at, is_active, granted_by_request_id) VALUES
    (5, 1, 4, false, '2020-01-01', NULL, true, NULL),
    (5, 2, 4, false, '2020-01-01', NULL, true, NULL),
    (5, 3, 4, false, '2020-01-01', NULL, true, NULL);
-- Mehmet (2): rol yetkileri + ERP istisna (request 2) + Eski Proje kapalı + Office kapalı
INSERT INTO personnel_accesses (personnel_id, resource_system_id, permission_type, is_exception, granted_at, expires_at, is_active, granted_by_request_id) VALUES
    (2, 1, 0, false, '2022-03-15', NULL, true, NULL),
    (2, 1, 1, false, '2022-03-15', NULL, true, NULL),
    (2, 3, 0, false, '2022-03-15', NULL, true, NULL),
    (2, 6, 0, false, '2022-03-15', NULL, true, NULL),
    (2, 2, 0, false, '2022-03-15', NULL, true, NULL),
    (2, 4, 0, false, '2022-03-15', NULL, true, NULL),
    (2, 5, 0, true, (CURRENT_TIMESTAMP - INTERVAL '30 days'), (CURRENT_TIMESTAMP + INTERVAL '30 days'), true, 2),
    (2, 7, 4, false, (CURRENT_TIMESTAMP - INTERVAL '12 months'), NULL, false, NULL),
    (2, 4, 4, false, '2022-03-15', NULL, false, NULL);
-- Ayşe (3): VPN kapalı
INSERT INTO personnel_accesses (personnel_id, resource_system_id, permission_type, is_exception, granted_at, expires_at, is_active, granted_by_request_id) VALUES
    (3, 3, 4, false, '2023-06-01', NULL, false, NULL);

-- -----------------------------------------------------------------------------
-- 10. audit_logs
-- -----------------------------------------------------------------------------
INSERT INTO audit_logs (actor_id, actor_name, action, target_type, target_id, details, "timestamp") VALUES
    (1, 'Enes Emre Arıkan', 5, 'Personnel', '3', 'İşe giriş', '2023-06-01'),
    (1, 'Enes Emre Arıkan', 6, 'PersonnelAccess', '1', 'Bitbucket Write', (CURRENT_TIMESTAMP - INTERVAL '3 days')),
    (1, 'Enes Emre Arıkan', 8, 'Personnel', '5', 'İşten çıkış', '2024-11-30'),
    (3, 'Ayşe Demir', 9, 'AccessRequest', '3', 'Test Ortamları Write talebi', CURRENT_TIMESTAMP);

INSERT INTO audit_logs (actor_id, actor_name, action, target_type, target_id, details, "timestamp")
SELECT
    1,
    'Enes Emre Arıkan',
    (i - 1) % 5,
    CASE WHEN i % 2 = 0 THEN 'Personnel' ELSE 'Access' END,
    (i)::text,
    'Örnek işlem ' || (i - 1),
    CURRENT_TIMESTAMP - (i || ' days')::interval
FROM generate_series(1, 15) AS i;

-- -----------------------------------------------------------------------------
-- 11. assets
-- -----------------------------------------------------------------------------
INSERT INTO assets (id, asset_type, name, serial_number, brand_model, status, notes, purchase_date) VALUES
    (1, 0, 'Dizüstü-001', 'SN-LAP-001', 'Dell Latitude 5520', 1, NULL, '2022-01-15'),
    (2, 0, 'Dizüstü-002', 'SN-LAP-002', 'HP EliteBook 840', 1, NULL, '2023-03-01'),
    (3, 0, 'Dizüstü-003', 'SN-LAP-003', 'Lenovo ThinkPad X1', 0, NULL, '2024-01-10'),
    (4, 3, 'Telefon-001', 'SN-PH-001', 'iPhone 14', 1, NULL, '2023-06-01'),
    (5, 3, 'Telefon-002', 'SN-PH-002', 'Samsung Galaxy S23', 0, NULL, '2024-02-01'),
    (6, 2, 'Monitör-001', 'SN-MON-001', 'Dell P2422H', 1, NULL, '2021-08-01');

-- -----------------------------------------------------------------------------
-- 12. asset_assignments
-- -----------------------------------------------------------------------------
INSERT INTO asset_assignments (id, asset_id, personnel_id, assigned_at, assigned_by_user_id, assigned_by_user_name, returned_at, return_condition, notes) VALUES
    (1, 1, 2, '2022-03-16', 1, 'Enes Emre Arıkan', NULL, NULL, 'İşe giriş donanımı'),
    (2, 2, 3, '2023-06-02', 1, 'Enes Emre Arıkan', NULL, NULL, 'İşe giriş donanımı'),
    (3, 4, 2, (CURRENT_TIMESTAMP - INTERVAL '6 months'), 1, 'Enes Emre Arıkan', NULL, NULL, 'Kurumsal telefon'),
    (4, 6, 4, '2021-09-03', 1, 'Enes Emre Arıkan', NULL, NULL, NULL);

-- -----------------------------------------------------------------------------
-- 13. asset_assignment_notes
-- -----------------------------------------------------------------------------
INSERT INTO asset_assignment_notes (asset_assignment_id, content, created_at, created_by_user_id, created_by_user_name) VALUES
    (1, 'Laptop teslim alındı, şifre ayarlandı.', CURRENT_TIMESTAMP, 1, 'Enes Emre Arıkan');

-- -----------------------------------------------------------------------------
-- 14. personnel_notes
-- -----------------------------------------------------------------------------
INSERT INTO personnel_notes (personnel_id, content, created_at, created_by_user_id, created_by_user_name) VALUES
    (2, 'Yıllık performans görüşmesi tamamlandı.', CURRENT_TIMESTAMP, 1, 'Enes Emre Arıkan');

-- -----------------------------------------------------------------------------
-- Sequence'leri güncelle (explicit id kullandığımız için sonraki INSERT'ler doğru id alsın)
-- -----------------------------------------------------------------------------
SELECT setval(pg_get_serial_sequence('departments', 'id'), (SELECT COALESCE(MAX(id), 1) FROM departments));
SELECT setval(pg_get_serial_sequence('roles', 'id'), (SELECT COALESCE(MAX(id), 1) FROM roles));
SELECT setval(pg_get_serial_sequence('personnel', 'id'), (SELECT COALESCE(MAX(id), 1) FROM personnel));
SELECT setval(pg_get_serial_sequence('resource_systems', 'id'), (SELECT COALESCE(MAX(id), 1) FROM resource_systems));
SELECT setval(pg_get_serial_sequence('app_users', 'id'), (SELECT COALESCE(MAX(id), 1) FROM app_users));
SELECT setval(pg_get_serial_sequence('role_permissions', 'id'), (SELECT COALESCE(MAX(id), 1) FROM role_permissions));
SELECT setval(pg_get_serial_sequence('access_requests', 'id'), (SELECT COALESCE(MAX(id), 1) FROM access_requests));
SELECT setval(pg_get_serial_sequence('approval_steps', 'id'), (SELECT COALESCE(MAX(id), 1) FROM approval_steps));
SELECT setval(pg_get_serial_sequence('personnel_accesses', 'id'), (SELECT COALESCE(MAX(id), 1) FROM personnel_accesses));
SELECT setval(pg_get_serial_sequence('audit_logs', 'id'), (SELECT COALESCE(MAX(id), 1) FROM audit_logs));
SELECT setval(pg_get_serial_sequence('assets', 'id'), (SELECT COALESCE(MAX(id), 1) FROM assets));
SELECT setval(pg_get_serial_sequence('asset_assignments', 'id'), (SELECT COALESCE(MAX(id), 1) FROM asset_assignments));
SELECT setval(pg_get_serial_sequence('asset_assignment_notes', 'id'), (SELECT COALESCE(MAX(id), 1) FROM asset_assignment_notes));
SELECT setval(pg_get_serial_sequence('personnel_notes', 'id'), (SELECT COALESCE(MAX(id), 1) FROM personnel_notes));
