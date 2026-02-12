-- Mevcut veritabanından sicil_no kaldırma (personel artık sadece id ile tanımlanır).
-- Yeni kurulumlarda 01_create_tables.sql zaten sicil_no içermiyor; bu script sadece eski DB için.

ALTER TABLE personnel DROP CONSTRAINT IF EXISTS uq_personnel_sicil_no;
ALTER TABLE personnel DROP COLUMN IF EXISTS sicil_no;
