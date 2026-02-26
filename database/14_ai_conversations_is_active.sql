-- =============================================================================
-- ai_conversations tablosuna is_active kolonu (silinen sohbetler listelenmez)
-- =============================================================================

ALTER TABLE ai_conversations
    ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT true;

COMMENT ON COLUMN ai_conversations.is_active IS 'false ise sohbet silindi sayılır; listede gösterilmez';
