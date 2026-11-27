ALTER TABLE workspaces
    ADD COLUMN IF NOT EXISTS is_archived boolean NOT NULL DEFAULT false;
