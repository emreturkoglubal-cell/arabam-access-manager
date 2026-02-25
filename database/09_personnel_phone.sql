-- Personel tablosuna telefon numarası alanı (nullable).

ALTER TABLE personnel
    ADD COLUMN IF NOT EXISTS phone VARCHAR(50);

COMMENT ON COLUMN personnel.phone IS 'Telefon numarası';
