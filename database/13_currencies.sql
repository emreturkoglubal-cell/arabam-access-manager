-- Para birimi kurları (baz: USD). Dönüşüm: amount_usd = amount_currency * rate_to_usd

CREATE TABLE IF NOT EXISTS currencies (
    code            VARCHAR(3) PRIMARY KEY,
    rate_to_usd     NUMERIC(18, 8) NOT NULL,
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE currencies IS 'Para birimi kurları; baz birim USD. rate_to_usd: 1 birim bu para = kaç USD';

-- 1 USD = 1 USD
INSERT INTO currencies (code, rate_to_usd) VALUES ('USD', 1)
ON CONFLICT (code) DO UPDATE SET rate_to_usd = EXCLUDED.rate_to_usd, updated_at = now();

-- 1 USD = 43,88 TRY  =>  1 TRY = 1/43.88 USD
INSERT INTO currencies (code, rate_to_usd) VALUES ('TRY', 1.0 / 43.88)
ON CONFLICT (code) DO UPDATE SET rate_to_usd = EXCLUDED.rate_to_usd, updated_at = now();

-- 1 EUR = 1,18 USD
INSERT INTO currencies (code, rate_to_usd) VALUES ('EUR', 1.18)
ON CONFLICT (code) DO UPDATE SET rate_to_usd = EXCLUDED.rate_to_usd, updated_at = now();
