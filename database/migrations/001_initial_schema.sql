-- ============================================================================
-- 001_initial_schema.sql
-- PostgreSQL schema for the AI Workflow Automation Platform
-- Compatible with Neon DB (serverless Postgres)
-- ============================================================================

-- Enable the uuid-ossp extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- WORKFLOWS
-- Root aggregate table storing each contract-analysis workflow instance.
-- ============================================================================
CREATE TABLE IF NOT EXISTS workflows (
    id              UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),
    status          VARCHAR(50)     NOT NULL DEFAULT 'Pending'
                                    CHECK (status IN (
                                        'Pending', 'Running', 'AwaitingApproval',
                                        'Approved', 'Completed', 'Failed', 'Rejected'
                                    )),
    input_data      JSONB           NOT NULL,
    result          JSONB,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE  workflows             IS 'Contract analysis workflow instances';
COMMENT ON COLUMN workflows.id          IS 'Unique workflow identifier (UUIDv4)';
COMMENT ON COLUMN workflows.status      IS 'Current lifecycle status of the workflow';
COMMENT ON COLUMN workflows.input_data  IS 'ContractInput JSON: provider, currentPrice, duration, planType, customerName';
COMMENT ON COLUMN workflows.result      IS 'WorkflowResult JSON: recommendation, reasoning, suggestedOffer, estimatedSavings, analyzedAt';
COMMENT ON COLUMN workflows.created_at  IS 'Timestamp when the workflow was created';
COMMENT ON COLUMN workflows.updated_at  IS 'Timestamp of the most recent status change';

-- Index on status for dashboard filtering and queue queries
CREATE INDEX IF NOT EXISTS idx_workflows_status ON workflows (status);

-- Index on created_at for chronological listing
CREATE INDEX IF NOT EXISTS idx_workflows_created_at ON workflows (created_at DESC);

-- ============================================================================
-- WORKFLOW STEPS
-- Individual executable steps within a workflow, tracked independently.
-- ============================================================================
CREATE TABLE IF NOT EXISTS workflow_steps (
    id              UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),
    workflow_id     UUID            NOT NULL REFERENCES workflows(id) ON DELETE CASCADE,
    name            VARCHAR(200)    NOT NULL,
    status          VARCHAR(50)     NOT NULL DEFAULT 'Pending'
                                    CHECK (status IN (
                                        'Pending', 'Running', 'Completed', 'Failed', 'Skipped'
                                    )),
    output          TEXT,
    started_at      TIMESTAMPTZ,
    completed_at    TIMESTAMPTZ,
    step_order      INT             NOT NULL DEFAULT 0,

    CONSTRAINT uq_workflow_step_order UNIQUE (workflow_id, step_order)
);

COMMENT ON TABLE  workflow_steps                IS 'Individual steps within a workflow';
COMMENT ON COLUMN workflow_steps.workflow_id     IS 'Parent workflow FK';
COMMENT ON COLUMN workflow_steps.name            IS 'Human-readable step name (e.g. "AI Contract Analysis")';
COMMENT ON COLUMN workflow_steps.step_order      IS 'Execution order within the parent workflow (1-based)';
COMMENT ON COLUMN workflow_steps.output          IS 'Step output or error message';

-- Index for fast lookup of steps by workflow
CREATE INDEX IF NOT EXISTS idx_workflow_steps_workflow_id ON workflow_steps (workflow_id);

-- Index for ordered retrieval
CREATE INDEX IF NOT EXISTS idx_workflow_steps_order ON workflow_steps (workflow_id, step_order);

-- ============================================================================
-- OFFERS
-- Cached energy offers scraped from provider portals.
-- ============================================================================
CREATE TABLE IF NOT EXISTS offers (
    id              UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider        VARCHAR(200)    NOT NULL,
    price           DECIMAL(10, 2)  NOT NULL CHECK (price >= 0),
    features        JSONB           NOT NULL DEFAULT '[]'::JSONB,
    plan_name       VARCHAR(200)    NOT NULL,
    plan_type       VARCHAR(50)     NOT NULL CHECK (plan_type IN ('electricity', 'gas', 'dual')),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE  offers            IS 'Cached energy provider offers';
COMMENT ON COLUMN offers.provider   IS 'Energy provider company name';
COMMENT ON COLUMN offers.price      IS 'Monthly price in the local currency';
COMMENT ON COLUMN offers.features   IS 'JSON array of plan feature strings';
COMMENT ON COLUMN offers.plan_name  IS 'Marketing name of the plan';
COMMENT ON COLUMN offers.plan_type  IS 'Energy type: electricity, gas, or dual';

-- Index for type-based filtering (used in AI analysis queries)
CREATE INDEX IF NOT EXISTS idx_offers_plan_type ON offers (plan_type);

-- Index for provider-based lookups
CREATE INDEX IF NOT EXISTS idx_offers_provider ON offers (provider);

-- ============================================================================
-- TRIGGER: auto-update updated_at on workflows
-- ============================================================================
CREATE OR REPLACE FUNCTION update_workflows_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_workflows_updated_at ON workflows;
CREATE TRIGGER trg_workflows_updated_at
    BEFORE UPDATE ON workflows
    FOR EACH ROW
    EXECUTE FUNCTION update_workflows_updated_at();

-- ============================================================================
-- SEED DATA: Sample energy offers
-- ============================================================================
INSERT INTO offers (id, provider, price, features, plan_name, plan_type) VALUES
    (uuid_generate_v4(), 'GreenGrid',   72.00,  '["Solar offset credits", "Carbon neutral", "Battery storage discount"]'::JSONB,        'Solar Plus',          'electricity'),
    (uuid_generate_v4(), 'GreenGrid',   65.00,  '["Fixed rate", "Free boiler check", "Emergency callout"]'::JSONB,                      'Gas Comfort',         'gas'),
    (uuid_generate_v4(), 'PowerCorp',   78.00,  '["Fixed rate guarantee", "Dedicated account manager", "Priority support"]'::JSONB,     'Business Fixed 24',   'electricity'),
    (uuid_generate_v4(), 'PowerCorp',   88.00,  '["Fixed rate", "Paper-free billing", "Energy usage dashboard"]'::JSONB,                'Home Essential',      'electricity'),
    (uuid_generate_v4(), 'TestEnergy',  85.00,  '["100% renewable", "No exit fees", "Smart meter included"]'::JSONB,                    'Green Saver 12',      'electricity'),
    (uuid_generate_v4(), 'TestEnergy',  92.50,  '["100% renewable", "Flexible contract", "Online management"]'::JSONB,                  'Eco Flex',            'electricity'),
    (uuid_generate_v4(), 'EcoWatt',     69.00,  '["Wind powered", "Community solar", "Carbon offset program"]'::JSONB,                  'Wind Basic',          'electricity'),
    (uuid_generate_v4(), 'EcoWatt',     58.00,  '["Budget plan", "Variable rate", "No lock-in"]'::JSONB,                                'Gas Lite',            'gas')
ON CONFLICT DO NOTHING;
