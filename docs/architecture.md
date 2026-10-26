# Architecture

## 1. Products service in an event-driven microservices landscape

```mermaid
flowchart LR
    subgraph Clients
        SPA[Angular SPA]
        Mobile[Mobile / Partner apps]
    end

    IdP[(Identity Provider<br/>Entra ID / Auth0 / Keycloak)]
    GW[API Gateway / BFF<br/>TLS, rate limiting, routing]

    subgraph Services
        direction TB
        PS[Products Service<br/>.NET Web API]
        OS[Orders Service]
        PAY[Payments Service]
        INV[Inventory Service]
        NS[Notifications Service]
        SS[Search Service]
    end

    subgraph Data["Data stores (database per service)"]
        PDB[(Products DB<br/>+ Outbox table)]
        ODB[(Orders DB)]
        PAYDB[(Payments DB)]
        INVDB[(Inventory DB)]
        IDX[(Search index)]
    end

    BUS{{Message broker<br/>Azure Service Bus / RabbitMQ / Kafka}}
    PSP[[External payment provider<br/>Stripe / Adyen]]

    SPA -- OIDC login --> IdP
    SPA -- HTTPS + JWT --> GW
    Mobile -- HTTPS + JWT --> GW
    GW --> PS & OS & PAY

    PS --- PDB
    OS --- ODB
    PAY --- PAYDB
    INV --- INVDB
    SS --- IDX

    PS -- "ProductCreated / ProductUpdated<br/>(via transactional outbox)" --> BUS
    OS -- "OrderPlaced / OrderCancelled" --> BUS
    PAY -- "PaymentSucceeded / PaymentFailed" --> BUS
    INV -- "StockReserved / StockReservationFailed" --> BUS

    BUS -- "Product*" --> OS
    BUS -- "Product*" --> INV
    BUS -- "Product*" --> SS
    BUS -- "OrderPlaced" --> PAY
    BUS -- "OrderPlaced" --> INV
    BUS -- "Payment* / Stock*" --> OS
    BUS -- "OrderConfirmed / PaymentFailed" --> NS

    PAY <--> PSP
```

### How the pieces interact

| Component | Responsibility | Publishes | Subscribes to |
|---|---|---|---|
| **Products** (this repo) | Owns the product catalogue (source of truth for name, colour, price). | `ProductCreated` (and later `ProductUpdated`, `ProductDiscontinued`) | – |
| **Orders** | Order lifecycle; keeps a local read-model of products for pricing snapshots, so it never calls Products synchronously when placing an order. | `OrderPlaced`, `OrderConfirmed`, `OrderCancelled` | `Product*`, `Payment*`, `Stock*` |
| **Payments** | Takes payment through the external PSP and is idempotent on `OrderId`. | `PaymentSucceeded`, `PaymentFailed` | `OrderPlaced` |
| **Inventory** | Stock levels and reservations. | `StockReserved`, `StockReservationFailed` | `ProductCreated`, `OrderPlaced` |
| **Search** | Denormalised, searchable catalogue (e.g. filter by colour at scale). | – | `Product*` |
| **Notifications** | Email/SMS to customers. | – | `OrderConfirmed`, `PaymentFailed` |

### Example flow: placing an order (choreographed saga)

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant O as Orders
    participant B as Message broker
    participant I as Inventory
    participant P as Payments
    participant N as Notifications

    C->>O: POST /orders (JWT)
    O->>O: Save order (Pending) + outbox(OrderPlaced)
    O-->>C: 202 Accepted
    O-)B: OrderPlaced
    B-)I: OrderPlaced
    I-)B: StockReserved
    B-)P: OrderPlaced
    P->>P: Charge via PSP (idempotency key = OrderId)
    P-)B: PaymentSucceeded
    B-)O: StockReserved + PaymentSucceeded
    O->>O: Order -> Confirmed + outbox(OrderConfirmed)
    O-)B: OrderConfirmed
    B-)N: OrderConfirmed
    Note over O,I: On PaymentFailed, Orders cancels the order and<br/>Inventory releases the reservation (compensation).
```

### Key design decisions

- **Database per service.** No service reads another service's database. Data is shared only through APIs and events.
- **Transactional outbox.** The Products service writes the `ProductCreated` event to an `OutboxMessages` table in the *same* transaction as the product. A background dispatcher then publishes it to the broker. This avoids the "dual write" problem, where the DB commit succeeds but the publish fails, and gives at-least-once delivery.
- **Idempotent consumers.** Events carry a unique `eventId` (UUIDv7). Consumers record the ids they have processed so that redelivered events have no further effect.
- **Choreography over orchestration** for simple flows. A process manager such as MassTransit sagas or Durable Functions can be introduced once flows become complex.
- **Security at the edge and in every service.** The gateway terminates TLS and applies coarse rate limiting. Each service still validates the JWT (issuer, audience, signature, lifetime) and enforces scope-based policies such as `products.read` and `products.write` (zero trust).
- **Observability.** Use OpenTelemetry traces, metrics and logs, propagating `traceparent` across HTTP and message headers. Every service exposes `/health/live` and `/health` for orchestrator liveness and readiness probes.

## 2. Internal structure of the Products service

```mermaid
flowchart TB
    subgraph Api["Products.Api (ASP.NET Core)"]
        C[Controllers<br/>Auth, Products]
        MW[Middleware<br/>ProblemDetails, AuthN/Z, CORS, Rate limiting]
        H[Health checks]
    end
    subgraph App["Products.Application"]
        S[ProductService]
        V[FluentValidation validators]
        A[Abstractions<br/>IProductRepository, IUnitOfWork]
    end
    subgraph Dom["Products.Domain"]
        E[Product aggregate<br/>invariants + domain events]
    end
    subgraph Infra["Products.Infrastructure"]
        DB[EF Core DbContext<br/>+ Repository]
        OB[Outbox dispatcher<br/>BackgroundService]
        MP[IMessagePublisher]
    end

    C --> S
    S --> V
    S --> A
    S --> E
    DB -. implements .-> A
    DB --> SQL[(SQLite / SQL Server)]
    OB --> DB
    OB --> MP --> BUS{{Broker}}
```

Dependencies point inwards (Clean Architecture). The Domain has no dependencies. The Application layer depends only on the Domain. Infrastructure and Api sit on the outside.
