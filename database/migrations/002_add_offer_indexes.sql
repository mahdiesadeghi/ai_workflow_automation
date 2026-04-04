-- ============================================================================
-- 002_add_offer_indexes.sql
-- Additional indexes to support common search and reporting queries.
-- ============================================================================

-- Composite index for the primary offer search query:
-- "Find offers of a given plan_type with price at or below a threshold"
CREATE INDEX IF NOT EXISTS idx_offers_type_price
    ON offers (plan_type, price);

-- GIN index on the features JSONB column to allow fast containment queries
-- e.g. WHERE features @> '["100% renewable"]'
CREATE INDEX IF NOT EXISTS idx_offers_features_gin
    ON offers USING GIN (features);

-- Index on provider + plan_type for provider-scoped filtering
CREATE INDEX IF NOT EXISTS idx_offers_provider_type
    ON offers (provider, plan_type);

-- Partial index for active/recent offers if a soft-delete or expiry column
-- is added later. For now, index on created_at for freshness queries.
CREATE INDEX IF NOT EXISTS idx_offers_created_at
    ON offers (created_at DESC);

-- Full-text search support on plan_name for free-text offer searches
CREATE INDEX IF NOT EXISTS idx_offers_plan_name_trgm
    ON offers USING GIN (plan_name gin_trgm_ops);

-- The trigram index above requires the pg_trgm extension.
-- Enable it (idempotent) before creating the index.
-- Note: on Neon DB, extensions are enabled per-database.
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Composite index on workflows for dashboard queries that filter by status
-- and sort by creation date.
CREATE INDEX IF NOT EXISTS idx_workflows_status_created
    ON workflows (status, created_at DESC);

-- Index on workflow_steps status for monitoring queries
-- (e.g. "find all currently running steps").
CREATE INDEX IF NOT EXISTS idx_workflow_steps_status
    ON workflow_steps (status);
