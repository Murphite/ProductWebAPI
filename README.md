# Products Web API

A production-style **.NET 10 Products API** with an **Angular 22** front end.

[![CI](../../actions/workflows/ci.yml/badge.svg)](../../actions/workflows/ci.yml)

| Requirement | Where |
|---|---|
| Anonymous health check endpoint | `GET /health` (readiness, incl. DB) and `GET /health/live` (liveness) |
| Secured: create a product | `POST /api/v1/products` (requires the `products.write` scope) |
| Secured: list all products as JSON | `GET /api/v1/products` (requires the `products.read` scope) |
| Secured: products of a specific colour | `GET /api/v1/products?colour=red` (case-insensitive) |
| Unit and integration tests | [tests/](tests) (xUnit) and [client/src/**/*.spec.ts](client/src) (Vitest) |
| Architecture diagram | [docs/architecture.md](docs/architecture.md) |
| Front end | [client/](client) (Angular) |

## Quick start

### Option A: Docker (everything in one command)

```bash
docker compose up --build
```

Open http://localhost:8080 and sign in with `admin` / `Passw0rd!` (read and write) or `viewer` / `Passw0rd!` (read-only).

### Option B: Run locally

Prerequisites: [.NET SDK 10](https://dotnet.microsoft.com/download) and [Node.js 22+](https://nodejs.org/).

```bash
# 1. API: http://localhost:5157 (creates and migrates products.db on first run)
dotnet run --project src/Products.Api --launch-profile http

# 2. Front end: http://localhost:4200 (proxies /api to the API)
cd client
npm install
npm start
```

In Development the interactive API reference (Scalar) is available at http://localhost:5157/scalar, and the OpenAPI document is at `/openapi/v1.json`.

### Try it with curl

```bash
TOKEN=$(curl -s -X POST http://localhost:5157/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Passw0rd!"}' | jq -r .accessToken)

curl -X POST http://localhost:5157/api/v1/products \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Fire truck","colour":"Red","price":49.99,"description":"Toy"}'

curl http://localhost:5157/api/v1/products -H "Authorization: Bearer $TOKEN"
curl "http://localhost:5157/api/v1/products?colour=red" -H "Authorization: Bearer $TOKEN"
curl http://localhost:5157/health
```

A ready-made [Products.Api.http](src/Products.Api/Products.Api.http) file is included for VS Code / Visual Studio / Rider.

## Running the tests

```bash
dotnet test                              # 39 unit + 20 integration tests
cd client && npx ng test --watch=false   # 18 front-end tests
```

- **Unit tests** cover:
  - domain invariants and events
  - validation rules
  - `ProductService` (with NSubstitute and `FakeTimeProvider`)
  - JWT issuance
  - the outbox processor (success, failure/retry, max attempts)
- **Integration tests** boot the real HTTP pipeline with `WebApplicationFactory`, against an isolated, throw-away SQLite database per test class. They cover:
  - health endpoints
  - 401 responses for missing, expired or forged tokens
  - 403 when the user lacks the right scope
  - 201 + `Location` on create
  - 400 problem details for invalid or malformed input
  - listing, colour filtering (case- and whitespace-insensitive), 404s
  - events written to the outbox
- **Front-end tests** cover:
  - the auth service (session, expiry, scopes)
  - the HTTP interceptor (token attachment only to same-origin `/api`, logout on 401)
  - the products service
  - the products page (rendering, debounced filtering, empty and error states, permissions)

## API

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/health/live` | Anonymous | Liveness. The process is up. |
| `GET` | `/health` | Anonymous | Readiness, including a database check (JSON body). |
| `POST` | `/api/v1/auth/token` | Anonymous (rate-limited) | Exchange demo credentials for a JWT. |
| `GET` | `/api/v1/products[?colour=]` | `products.read` | List all products, optionally by colour. |
| `GET` | `/api/v1/products/{id}` | `products.read` | Get one product. |
| `POST` | `/api/v1/products` | `products.write` | Create a product: `201 Created` + `Location`. |

Example product:

```json
{
  "id": "01a17876-b7a3-756a-867c-622bbed2a7a7",
  "name": "Fire truck",
  "description": "Toy",
  "colour": "Red",
  "price": 49.99,
  "createdAt": "2026-10-26T13:46:48.611+00:00"
}
```

Errors use [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`) and include a `traceId`. Unexpected exceptions are logged server-side and never leak stack traces.

## Solution structure

```
src/
  Products.Domain/          Product aggregate, invariants, domain events (no dependencies)
  Products.Application/     Use cases (ProductService), validation, persistence abstractions
  Products.Infrastructure/  EF Core (SQLite), repository, transactional outbox + dispatcher, health checks
  Products.Api/             Controllers, JWT auth, ProblemDetails, CORS, rate limiting, OpenAPI
tests/
  Products.UnitTests/
  Products.IntegrationTests/
client/                     Angular 22 SPA (standalone components, signals, zoneless)
docs/architecture.md        Distributed / event-driven architecture diagrams
```

## Design notes

**Clean Architecture.** Dependencies point inwards. Business rules live in the `Product` aggregate (`Product.Create` enforces invariants). Request validation (FluentValidation) gives callers field-level errors before the domain is touched.

**Security.**
- JWT bearer authentication validates issuer, audience, signature and lifetime with a 30-second clock skew.
- Authorization is scope-based (`products.read` / `products.write`), with a **secure-by-default fallback policy**: any new endpoint requires authentication unless it explicitly opts out with `[AllowAnonymous]`.
- The token endpoint is rate-limited per IP and compares credentials in constant time.
- CORS is restricted to configured origins, HSTS and HTTPS redirection are on outside Development, and the Docker images run as non-root.

> **Demo identity provider.** `POST /api/v1/auth/token` with users from configuration exists so the exercise is self-contained. In production this endpoint would be removed. The API would validate tokens from a real IdP (Entra ID / Auth0 / Keycloak) by setting `Authority`, and the SPA would use OIDC authorization code + PKCE. Signing keys and passwords belong in a secret store (Key Vault, user-secrets, env vars). The development values in `appsettings.Development.json` are not secrets.

**Event-driven readiness (transactional outbox).** Creating a product raises a `ProductCreatedEvent`. `ProductsDbContext.SaveChangesAsync` stores it in the `OutboxMessages` table *in the same transaction* as the product. `OutboxDispatcher` (a `BackgroundService`) publishes pending messages through `IMessagePublisher`, with retry and max-attempt tracking. The bundled publisher logs the event; swap in an Azure Service Bus / RabbitMQ / Kafka implementation (e.g. via MassTransit) without touching domain or application code. See [docs/architecture.md](docs/architecture.md) for how Orders, Payments, Inventory and other services consume these events.

**Persistence.**
- EF Core with **migrations** (applied at startup only when `Database:MigrateOnStartup` is enabled; in a real pipeline they run as a deployment step).
- Colour filtering uses an indexed, upper-cased `NormalizedColour` column, so lookups are case-insensitive and index-friendly.
- IDs are time-ordered UUIDv7.
- SQLite keeps the exercise dependency-free. Switching to SQL Server or PostgreSQL means changing the provider and connection string.

**Operational concerns.**
- Structured logging uses source-generated `LoggerMessage` methods.
- Separate liveness and readiness health checks.
- Options are validated on start, so the app fails fast on a missing or short JWT signing key.
- `TimeProvider` is injected for testability.
- Warnings are treated as errors, with the latest .NET analyzers enabled.

**Front end.**
- Angular standalone components with signals, `OnPush`, zoneless change detection and lazy-loaded routes.
- A functional HTTP interceptor attaches the token only to same-origin `/api` calls.
- A route guard protects the products page, and `returnUrl` is checked to prevent open redirects.
- Reactive forms mirror the server's validation rules, and server problem details are surfaced to the user.
- The colour filter is debounced, and `switchMap` cancels in-flight requests so stale results never overwrite newer ones.
- The create form is shown only to users with the write scope.

## Possible next steps

- Pagination (`?page=&pageSize=` or cursor-based) once the catalogue grows. The list endpoint currently returns all products, as the brief asks.
- Update and delete endpoints, plus optimistic concurrency (ETags / row versions).
- OpenTelemetry tracing and metrics exported to an APM tool.
- A broker-backed `IMessagePublisher` and an inbox table for idempotent consumers.
- Replace the demo token endpoint with an external IdP.
