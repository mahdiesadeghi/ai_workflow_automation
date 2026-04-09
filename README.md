# AI-Native Workflow Automation Platform

A simplified prototype of an AI-native internal platform that automates subscription management workflows (e.g., energy contracts). The system demonstrates workflow orchestration, human-in-the-loop AI, and configurable architecture.

## Architecture

```
┌─────────────────┐     ┌──────────────────────────┐     ┌───────────────┐
│  Angular 17 UI  │────▶│  .NET 8 Web API          │────▶│  PostgreSQL   │
│  (port 4200)    │     │  (port 5000)             │     │  (port 5432)  │
└─────────────────┘     │                          │     └───────┬───────┘
                        │  ┌────────────────────┐  │             │
                        │  │ Workflow Orchestrator│  │     ┌──────▼────────┐
                        │  │ (dual mode)         │  │     │  Windmill     │
                        │  └────────┬───────────┘  │     │  (port 8000)  │
                        │           │              │     └───────────────┘
                        │     ┌─────┴──────┐       │
                        │     │            │       │
                        │  ┌──▼───┐  ┌─────▼────┐  │
                        │  │.NET  │  │ Windmill  │  │
                        │  │local │  │ remote    │  │
                        │  └──────┘  └──────────┘  │
                        │                          │
                        │  ┌────────────────────┐  │
                        │  │ AI Analysis Service │  │
                        │  │ (Semantic Kernel)   │  │
                        │  └────────────────────┘  │
                        │                          │
                        │  ┌────────────────────┐  │
                        │  │ Provider Scraper    │  │
                        │  │ (Playwright mock)   │  │
                        │  └────────────────────┘  │
                        └──────────────────────────┘
```

## Tech Stack

| Layer      | Technology                                       |
|------------|--------------------------------------------------|
| Frontend   | Angular 17 (standalone components)               |
| Backend    | C# / .NET 8 Web API (Clean Architecture)         |
| AI         | Microsoft Semantic Kernel + OpenAI               |
| Workflow   | Windmill.dev (self-hosted) or .NET in-process    |
| Database   | PostgreSQL 16                                    |
| Automation | Playwright (browser scraping simulation)         |
| Logging    | Serilog (structured logging)                     |

## Core Use Case

1. User submits their energy contract details (provider, price, duration, plan type)
2. Backend orchestrates a multi-step workflow:
   - Input validation & normalization
   - Provider offer scraping (simulated)
   - AI-powered contract analysis
   - Decision making
   - Human approval gate
   - Execution (simulated provider switch)
3. AI recommends **keep** or **switch** with reasoning and estimated savings
4. User approves or rejects the recommendation
5. System executes the switch (simulated)

## Execution Modes

The platform supports two workflow execution modes, selectable per workflow from the UI:

- **.NET (In-Process)** — Runs all workflow steps locally within the .NET backend. No external dependencies required.
- **Windmill** — Delegates workflow steps to a self-hosted [Windmill](https://www.windmill.dev/) engine. Falls back gracefully to local .NET execution if Windmill is unreachable or a script is missing.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22+](https://nodejs.org/)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- PostgreSQL 16 (or use in-memory DB for dev)

## Quick Start

### 1. Start infrastructure (PostgreSQL + Windmill)

```bash
cp .env.example .env
docker compose up -d
```

This starts:
- **PostgreSQL** on port 5432 (with both `workflow_automation` and `windmill` databases)
- **Windmill server** on port 8000 (UI & API)
- **Windmill worker** (executes workflow scripts)

### 2. Run backend and frontend locally

**Backend:**
```bash
cd backend
dotnet restore
dotnet run --project src/WorkflowAutomation.Api
```

**Frontend:**
```bash
cd frontend
npm install
npm start
```

### 3. Deploy Windmill scripts (optional, for Windmill execution mode)

```bash
# Generate an API token in Windmill UI: http://localhost:8000 → Settings → Tokens
export WINDMILL_TOKEN=your_token
npx tsx windmill/deploy-scripts.ts
```

### Services

| Service          | URL                          |
|------------------|------------------------------|
| Frontend         | http://localhost:4200         |
| Backend API      | http://localhost:5000         |
| Swagger          | http://localhost:5000/swagger |
| Windmill UI      | http://localhost:8000         |

### Database (manual setup, alternative to Docker)

```bash
psql -h localhost -U postgres -d workflow_automation -f database/migrations/001_initial_schema.sql
psql -h localhost -U postgres -d workflow_automation -f database/migrations/002_add_offer_indexes.sql
```

Without PostgreSQL, the backend defaults to an in-memory database.

## API Endpoints

| Method | Endpoint                        | Description                          |
|--------|---------------------------------|--------------------------------------|
| POST   | `/api/workflows/start`          | Start a new analysis workflow        |
| GET    | `/api/workflows`                | List all workflows                   |
| GET    | `/api/workflows/{id}`           | Get workflow details                 |
| POST   | `/api/workflows/{id}/approve`   | Approve or reject a workflow         |
| GET    | `/api/offers`                   | List all available offers            |
| GET    | `/api/offers/search`            | Search offers by type and max price  |

### Example: Start a Workflow

```bash
curl -X POST http://localhost:5000/api/workflows/start \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "OldEnergy",
    "currentPrice": 110,
    "duration": 12,
    "planType": "electricity",
    "customerName": "Jane Doe",
    "executionMode": "dotnet"
  }'
```

The `executionMode` field accepts `"dotnet"` (default) or `"windmill"`.

### Example: Approve a Workflow

```bash
curl -X POST http://localhost:5000/api/workflows/{id}/approve \
  -H "Content-Type: application/json" \
  -d '{ "approved": true, "comment": "Looks good, proceed with switch" }'
```

## Project Structure

```
├── backend/
│   ├── backend.sln
│   ├── Dockerfile
│   ├── src/
│   │   ├── WorkflowAutomation.Domain/        # Entities, value objects, interfaces
│   │   ├── WorkflowAutomation.Application/    # Commands, queries, DTOs, validators
│   │   ├── WorkflowAutomation.Infrastructure/ # EF Core, AI service, orchestrators
│   │   │   └── Services/
│   │   │       ├── WorkflowOrchestrator.cs    # .NET in-process execution
│   │   │       ├── WindmillOrchestrator.cs    # Windmill remote execution
│   │   │       └── WindmillClient.cs          # Windmill REST API client
│   │   └── WorkflowAutomation.Api/            # Controllers, middleware, config
│   └── tests/
│       └── WorkflowAutomation.Tests/          # Unit tests
├── frontend/
│   ├── src/app/
│   │   ├── components/                        # Dashboard, NewWorkflow, WorkflowDetail
│   │   ├── services/                          # HTTP services
│   │   └── models/                            # TypeScript interfaces
│   ├── angular.json
│   └── Dockerfile
├── windmill/
│   ├── deploy-scripts.ts                      # Script deployment tool
│   └── scripts/workflows/                     # Windmill workflow step scripts
│       ├── input_validation.ts
│       ├── data_normalization.ts
│       ├── provider_scraping.ts
│       ├── ai_analysis.ts
│       ├── decision.ts
│       └── execution.ts
├── automation/
│   ├── scripts/provider-scraper.ts            # Playwright scraping simulation
│   └── tests/                                 # E2E tests
├── database/
│   ├── Dockerfile                             # Custom PostgreSQL image
│   ├── init/                                  # DB initialization scripts
│   │   └── 000_create_windmill_db.sh          # Creates Windmill database
│   └── migrations/                            # PostgreSQL schema & seed data
├── docker-compose.yml
└── .env.example
```

## Configuration

| Variable               | Description                          | Default                    |
|------------------------|--------------------------------------|----------------------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection      | In-memory DB               |
| `OpenAI__ApiKey`       | OpenAI API key for real AI           | Mock analysis used         |
| `WINDMILL_URL`         | Windmill server URL                  | `http://localhost:8000`    |
| `WINDMILL_TOKEN`       | Windmill API token                   | (required for Windmill mode) |
| `WINDMILL_WORKSPACE`   | Windmill workspace name              | `ai`                       |
| `WINDMILL_DB`          | Windmill database name               | `windmill`                 |

Without an OpenAI key, the AI analysis runs a deterministic mock that compares prices and recommends switching if savings exceed 10%.

## Workflow Steps

1. **Input Validation** - Verify contract data completeness
2. **Data Normalization** - Standardize pricing and plan data
3. **Provider Scraping** - Fetch competitor offers (Playwright mock)
4. **AI Analysis** - Semantic Kernel analyzes contract vs. alternatives
5. **Decision** - Determine keep/switch recommendation
6. **Human Approval** - Workflow pauses for user confirmation
7. **Execution** - Simulate provider switch (after approval)

## Testing

```bash
# Backend unit tests
cd backend && dotnet test

# Playwright E2E tests
cd automation && npx playwright test

# Frontend (Angular tests)
cd frontend && npm test
```

## Key Design Decisions

- **Clean Architecture**: Domain layer has zero dependencies; infrastructure details are abstracted behind interfaces
- **MediatR CQRS**: Commands and queries separated for clarity and testability
- **Dual execution modes**: Choose between .NET in-process or Windmill remote execution per workflow
- **Graceful fallback**: WindmillOrchestrator falls back to local .NET execution if Windmill is unreachable or a script is missing
- **Fire-and-forget orchestration**: Workflows run asynchronously; clients poll for status
- **Human-in-the-loop**: Workflow pauses at approval gate, resumes on user action
- **Graceful AI fallback**: Works without OpenAI key using deterministic comparison logic
- **In-memory DB fallback**: Runs without PostgreSQL for rapid development