ALTER TABLE users
    ADD COLUMN IF NOT EXISTS otp_secret text NULL,
    ADD COLUMN IF NOT EXISTS otp_pending_secret text NULL,
    ADD COLUMN IF NOT EXISTS otp_pending_expires_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS otp_last_used_step bigint NULL;
