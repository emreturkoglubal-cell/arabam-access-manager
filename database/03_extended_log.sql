-- =============================================================================
-- extended_log: Hata ve genel uygulama logları (AI, API vb.), IP, URL, context
-- =============================================================================

CREATE TABLE extended_logs (
    id           SERIAL PRIMARY KEY,
    level        VARCHAR(20) NOT NULL,
    source       VARCHAR(100) NOT NULL,
    message      TEXT NOT NULL,
    exception    TEXT,
    ip_address   VARCHAR(45),
    url          TEXT,
    http_method  VARCHAR(10),
    user_agent   TEXT,
    user_id      INT,
    user_name    VARCHAR(200),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    extra_data   TEXT,
    CONSTRAINT chk_extended_logs_level CHECK (level IN ('Error', 'Warning', 'Info'))
);

CREATE INDEX ix_extended_logs_created_at ON extended_logs (created_at DESC);
CREATE INDEX ix_extended_logs_source ON extended_logs (source);
CREATE INDEX ix_extended_logs_level ON extended_logs (level);
CREATE INDEX ix_extended_logs_user_id ON extended_logs (user_id) WHERE user_id IS NOT NULL;

COMMENT ON TABLE extended_logs IS 'Genişletilmiş hata/bilgi logları (AI, API vb.); IP, URL, kullanıcı bilgisi';
COMMENT ON COLUMN extended_logs.level IS 'Error, Warning, Info';
COMMENT ON COLUMN extended_logs.source IS 'Kaynak modül: Ai, Auth, Api vb.';
COMMENT ON COLUMN extended_logs.extra_data IS 'Ek bağlam (JSON veya serbest metin): conversation_id, request snippet vb.';
