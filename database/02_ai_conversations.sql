-- =============================================================================
-- AI sohbet: kullanıcı bazlı konuşmalar ve mesajlar
-- =============================================================================

-- -----------------------------------------------------------------------------
-- ai_conversations: Konuşma başlığı (ilk mesajın ilk N karakteri), kullanıcıya ait
-- -----------------------------------------------------------------------------
CREATE TABLE ai_conversations (
    id          SERIAL PRIMARY KEY,
    user_id     INT NOT NULL REFERENCES app_users (id) ON DELETE CASCADE,
    title       VARCHAR(200) NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_ai_conversations_user_id ON ai_conversations (user_id);
CREATE INDEX ix_ai_conversations_updated_at ON ai_conversations (user_id, updated_at DESC);

COMMENT ON TABLE ai_conversations IS 'AI sohbet konuşmaları (kullanıcı bazlı)';
COMMENT ON COLUMN ai_conversations.title IS 'İlk kullanıcı mesajından türetilen başlık (örn. ilk 80 karakter)';

-- -----------------------------------------------------------------------------
-- ai_conversation_messages: Her konuşmadaki mesajlar (user / assistant)
-- -----------------------------------------------------------------------------
CREATE TABLE ai_conversation_messages (
    id               SERIAL PRIMARY KEY,
    conversation_id  INT NOT NULL REFERENCES ai_conversations (id) ON DELETE CASCADE,
    role             VARCHAR(20) NOT NULL,
    content          TEXT NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_ai_message_role CHECK (role IN ('user', 'assistant'))
);

CREATE INDEX ix_ai_conversation_messages_conversation_id ON ai_conversation_messages (conversation_id);
CREATE INDEX ix_ai_conversation_messages_created_at ON ai_conversation_messages (conversation_id, created_at);

COMMENT ON TABLE ai_conversation_messages IS 'AI sohbet mesajları (user: kullanıcı, assistant: model cevabı)';
