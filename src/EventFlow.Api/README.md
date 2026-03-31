
# EventFlow

**EventFlow** is a lightweight distributed workflow orchestration engine built with **.NET 10, RabbitMQ, MySQL, Docker, and Clean Architecture**.

It demonstrates how modern backend systems coordinate execution using **events instead of direct service calls**, similar to platforms like **Temporal** or **Durable Functions** — but intentionally simplified for learning and experimentation.

Instead of:

API → Service → Service → Service

EventFlow runs workflows like this:

API → Event → Worker → Event → Worker → Event → Worker

Each stage executes independently, making workflows **observable, scalable, and resilient by design**.

---

# Why EventFlow exists

Most backend systems already run workflows:

- onboarding pipelines
- invoice processing
- report generation
- background automation
- integrations between services

But those workflows are usually hidden inside service calls.

EventFlow makes them explicit.

Each workflow becomes:

- event-driven
- stateful
- traceable
- replayable (foundation-ready)
- horizontally scalable

---

# Architecture Overview

EventFlow follows a **Clean Architecture layered design**:

EventFlow.Api  
EventFlow.Application  
EventFlow.Domain  
EventFlow.Infrastructure  
EventFlow.Worker.Validator  
EventFlow.Worker.Processor  
EventFlow.Worker.Notifications  
EventFlow.Worker.Audit  

Responsibilities are separated across layers:

| Layer | Responsibility |
|------|---------------|
| Domain | Workflow entities and lifecycle |
| Application | Use cases and orchestration contracts |
| Infrastructure | EF Core persistence + RabbitMQ messaging |
| Workers | Execution pipeline stages |
| API | Workflow entry point |

This keeps the system modular and production-style.

---

# Workflow Lifecycle

Each workflow moves through explicit lifecycle stages:

workflow.started  
workflow.validated  
workflow.processed  
workflow.completed  
workflow.failed  

Workers react to events and publish the next stage in the pipeline.

Example execution flow:

API  
→ workflow.started  
→ ValidatorWorker  
→ workflow.validated  
→ ProcessorWorker  
→ workflow.processed  
→ NotificationsWorker  
→ workflow.completed  

If validation fails:

workflow.started  
→ ValidatorWorker  
→ workflow.failed  
→ NotificationsWorker  

---

# Worker Responsibilities

ValidatorWorker

- validates payload
- applies routing decisions
- publishes success or failure events

ProcessorWorker

- executes workflow business logic
- transforms payload
- advances workflow execution

NotificationsWorker

- handles completion logic
- dispatches notifications
- integrates downstream systems

AuditWorker

- records execution timeline
- persists lifecycle transitions
- enables observability and debugging

---

# Event Timeline Tracking

Every workflow transition is stored inside:

WorkflowEventLogs

This creates a complete execution timeline showing:

- which worker handled each step
- which event was published
- when execution occurred
- retry attempts

This enables:

- debugging
- monitoring
- analytics
- future replay support

---

# Example Use Cases

EventFlow can orchestrate workflows like:

User onboarding

Create user  
→ Verify email  
→ Create workspace  
→ Assign permissions  
→ Send welcome email  

Invoice pipeline

Generate invoice  
→ Validate totals  
→ Charge payment  
→ Send receipt  
→ Notify accounting  

Report generation

Trigger report  
→ Load dataset  
→ Transform data  
→ Generate PDF  
→ Email result  

Automation pipelines

Import data  
→ Generate analytics  
→ Publish dashboard  
→ Notify stakeholders  

AI-agent coordination pipelines also fit naturally into this architecture.

---

# Tech Stack

.NET 10 Worker Services  
ASP.NET Core Web API  
RabbitMQ Topic Exchange  
MySQL  
EF Core (Pomelo Provider)  
Docker Compose  
Clean Architecture  

---

# Running EventFlow Locally

Start infrastructure

docker compose up -d mysql rabbitmq

Apply database migrations

dotnet ef migrations add InitialCreate --project src/EventFlow.Infrastructure --startup-project src/EventFlow.Api --output-dir Persistence/Migrations

dotnet ef database update --project src/EventFlow.Infrastructure --startup-project src/EventFlow.Api

Start all services

docker compose up --build

---

# Connect to MySQL inside Docker

mysql -u eventflow -peventflowpassword eventflow

---

# RabbitMQ Management UI

http://localhost:15672

Username: guest  
Password: guest

Use the UI to inspect:

- exchanges
- queues
- routing keys
- message flow

---

# What makes EventFlow interesting

EventFlow demonstrates how to:

design distributed workflow pipelines

replace service chaining with event orchestration

build observable execution timelines

scale execution horizontally using workers

apply clean architecture in a real distributed system

model workflows as lifecycle-driven state machines

This makes it a strong foundation for:

automation engines

integration pipelines

background processing platforms

AI-agent orchestration systems

learning event-driven architecture patterns

---

# Roadmap Ideas

Planned future improvements:

workflow handler plugin system

retry policies with exponential backoff

dead-letter queue support

workflow replay support

stage duration tracking

metrics export (OpenTelemetry / Prometheus)

dashboard UI

---

# Try It Yourself

Clone the repository and run the stack locally to see workflows move through the pipeline in real time.

Add your own workflow types.

Create new workers.

Extend lifecycle stages.

Experiment with automation pipelines.

EventFlow is designed to be easy to explore and modify so you can learn how distributed orchestration actually works under the hood.
