CREATE SCHEMA IF NOT EXISTS shared;

CREATE TABLE IF NOT EXISTS shared.outbox_messages (
    id              UUID PRIMARY KEY,
    event_type      TEXT NOT NULL,
    payload         JSONB NOT NULL,
    occurred_on     TIMESTAMPTZ NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at    TIMESTAMPTZ NULL,
    retry_count     INTEGER NOT NULL DEFAULT 0,
    last_error      TEXT NULL,
    source_module   TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS ix_outbox_unprocessed
    ON shared.outbox_messages (created_at ASC)
    WHERE processed_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_outbox_processed_cleanup
    ON shared.outbox_messages (processed_at)
    WHERE processed_at IS NOT NULL;
