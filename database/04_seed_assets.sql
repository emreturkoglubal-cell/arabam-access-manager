-- =============================================================================
-- Access Manager - Donanım & Zimmet örnek veri
-- 03_create_assets_tables.sql çalıştırıldıktan sonra kullanın.
-- Ön koşul: personnel tablosunda id 1,2,3,4 (Enes Emre Arıkan, Mehmet, Ayşe, Fatma) mevcut.
-- Zaten veri varsa bu INSERT'ler hata verebilir; gerekirse önce tabloları temizleyin.
-- =============================================================================

-- assets (0=Laptop, 1=Assigned vb. - status: 0=Available, 1=Assigned, 2=InRepair, 3=Retired)
INSERT INTO assets (id, asset_type, name, serial_number, brand_model, status, notes, purchase_date) VALUES
    (1, 0, 'Dizüstü-001', 'SN-LAP-001', 'Dell Latitude 5520', 1, NULL, '2022-01-15'),
    (2, 0, 'Dizüstü-002', 'SN-LAP-002', 'HP EliteBook 840', 1, NULL, '2023-03-01'),
    (3, 0, 'Dizüstü-003', 'SN-LAP-003', 'Lenovo ThinkPad X1', 0, NULL, '2024-01-10'),
    (4, 3, 'Telefon-001', 'SN-PH-001', 'iPhone 14', 1, NULL, '2023-06-01'),
    (5, 3, 'Telefon-002', 'SN-PH-002', 'Samsung Galaxy S23', 0, NULL, '2024-02-01'),
    (6, 2, 'Monitör-001', 'SN-MON-001', 'Dell P2422H', 1, NULL, '2021-08-01')
ON CONFLICT (id) DO NOTHING;

-- asset_assignments (personnel_id 2=Mehmet, 3=Ayşe, 4=Fatma)
INSERT INTO asset_assignments (id, asset_id, personnel_id, assigned_at, assigned_by_user_id, assigned_by_user_name, returned_at, return_condition, notes) VALUES
    (1, 1, 2, '2022-03-16', 1, 'Enes Emre Arıkan', NULL, NULL, 'İşe giriş donanımı'),
    (2, 2, 3, '2023-06-02', 1, 'Enes Emre Arıkan', NULL, NULL, 'İşe giriş donanımı'),
    (3, 4, 2, (CURRENT_TIMESTAMP - INTERVAL '6 months'), 1, 'Enes Emre Arıkan', NULL, NULL, 'Kurumsal telefon'),
    (4, 6, 4, '2021-09-03', 1, 'Enes Emre Arıkan', NULL, NULL, NULL)
ON CONFLICT (id) DO NOTHING;

-- asset_assignment_notes (PK id, serial - tek satır için conflict olmaz; yine de id verirsek ON CONFLICT kullanılabilir)
INSERT INTO asset_assignment_notes (asset_assignment_id, content, created_at, created_by_user_id, created_by_user_name) VALUES
    (1, 'Laptop teslim alındı, şifre ayarlandı.', CURRENT_TIMESTAMP, 1, 'Enes Emre Arıkan');

-- Sequence'leri güncelle
SELECT setval(pg_get_serial_sequence('assets', 'id'), (SELECT COALESCE(MAX(id), 1) FROM assets));
SELECT setval(pg_get_serial_sequence('asset_assignments', 'id'), (SELECT COALESCE(MAX(id), 1) FROM asset_assignments));
SELECT setval(pg_get_serial_sequence('asset_assignment_notes', 'id'), (SELECT COALESCE(MAX(id), 1) FROM asset_assignment_notes));
