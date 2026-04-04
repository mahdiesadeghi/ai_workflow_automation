# AI-Native Workflow Automation Platform

A simplified prototype of an AI-native internal platform that automates subscription management workflows (e.g., energy contracts). The system demonstrates workflow orchestration, human-in-the-loop AI, and configurable architecture.

## Architecture

```
┌─────────────────┐     ┌──────────────────────────┐     ┌───────────────┐
│  Angular 17 UI  │────▶│  .NET 8 Web API          │────▶│  PostgreSQL   │
│  (port 4200)    │     │  (port 5000)             │     │  (Neon DB)    │
└─────────────────┘     │                          │     └───────────────┘
                        │  ┌────────────────────┐  │
                        │  │ Workflow Orchestrator│  │
                        │  │ (Windmill mock)     │  │
                        │  └────────┬───────────┘  │
                        │           │              │
                        │  ┌────────▼───────────┐  │
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

| Layer      | Technology                                    |
|------------|-----------------------------------------------|
| Frontend   | Angular 17 (standalone components)            |
| Backend    | C# / .NET 8 Web API (Clean Architecture)      |
| AI         | Microsoft Semantic Kernel + OpenAI             |
| Workflow   | Windmill.dev (mocked orchestrator)             |
| Database   | PostgreSQL 16 / Neon DB                        |
| Automation | Playwright (browser scraping simulation)       |
| Logging    | Serilog (structured logging)                   |

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

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22+](https://nodejs.org/)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) (optional)
- PostgreSQL 16 (or use in-memory DB for dev)

## Quick Start

### Option 1: Docker Compose

```bash
cp .env.example .env
docker compose up --build
```

- Frontend: http://localhost:4200
- Backend API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

### Option 2: Manual Setup

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

**Database (optional):**
```bash
# Apply migrations to PostgreSQL
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
    "customerName": "Jane Doe"
  }'
```

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
│   │   ├── WorkflowAutomation.Infrastructure/ # EF Core, AI service, orchestrator
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
├── automation/
│   ├── scripts/provider-scraper.ts            # Playwright scraping simulation
│   └── tests/                                 # E2E tests
├── database/
│   └── migrations/                            # PostgreSQL schema & seed data
├── docker-compose.yml
└── .env.example
```

## Configuration

| Variable               | Description                    | Default              |
|------------------------|--------------------------------|----------------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection   | In-memory DB         |
| `OpenAI__ApiKey`       | OpenAI API key for real AI     | Mock analysis used   |
| `WINDMILL_URL`         | Windmill.dev base URL          | Mock orchestrator    |

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
- **Fire-and-forget orchestration**: Workflows run asynchronously; clients poll for status
- **Human-in-the-loop**: Workflow pauses at approval gate, resumes on user action
- **Graceful AI fallback**: Works without OpenAI key using deterministic comparison logic
- **In-memory DB fallback**: Runs without PostgreSQL for rapid development