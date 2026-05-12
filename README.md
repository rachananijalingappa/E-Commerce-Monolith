# ShopNova — E-Commerce Modular Monolith

A production-grade e-commerce platform built with **.NET 8**, demonstrating **Modular Monolith** architecture with CQRS, domain events, and an API Gateway.

> Clone → `dotnet run` → fully working storefront + API on `http://localhost:5000`

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-InMemory-blue)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Features

- **Modular Monolith** — 3 domain modules (Catalog, Orders, Basket) with schema-separated bounded contexts
- **CQRS** via MediatR — commands and queries with separate handlers
- **Domain Events** — cross-module communication through `OrderPlacedEvent` (Orders → Basket)
- **13 RESTful endpoints** — full CRUD on Catalog, cart management, order placement
- **FluentValidation** — automated request validation via MediatR pipeline behaviors
- **JWT Authentication** — Bearer token auth with Swagger integration
- **Ocelot API Gateway** — external route proxying with versioned URLs (`/api/v1/*`)
- **Observability** — Serilog structured logging with per-request Correlation IDs
- **Global Exception Handling** — consistent JSON error responses (400/500)
- **Health Checks** — JSON-formatted `/health` endpoint
- **Frontend UI** — glassmorphism storefront served from `wwwroot`

---

## Quick Start

```bash
# Clone this repository, then:
cd E-Commerce-Monolith
dotnet run --project Ecommerce.API
```

| URL | Description |
|---|---|
| http://localhost:5000 | Storefront UI |
| http://localhost:5000/swagger | Swagger API Explorer |
| http://localhost:5000/health | Health Check (JSON) |

No Docker, no SQL Server, no external dependencies — uses EF Core InMemory provider.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     API HOST (.NET 8)                        │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Middleware: ExceptionHandling → CorrelationID → Auth  │  │
│  └────────────────────────────────────────────────────────┘  │
│                           │                                  │
│      ┌────────────────────┼───────────────────┐              │
│      ▼                    ▼                   ▼              │
│ ┌──────────┐  ┌────────────────┐  ┌────────────────┐        │
│ │ Catalog  │  │    Orders      │  │    Basket      │        │
│ │ Module   │  │    Module      │  │    Module      │        │
│ │          │  │                │  │                │        │
│ │[Catalog] │  │   [Orders]     │  │   [Basket]     │        │
│ │ schema   │  │    schema      │  │    schema      │        │
│ └──────────┘  └───────┬────────┘  └───────▲────────┘        │
│                       │                   │                  │
│                       └───────────────────┘                  │
│                      OrderPlacedEvent                        │
│                     (MediatR pub/sub)                        │
└─────────────────────────────────────────────────────────────┘
```

---

## API Endpoints

### Auth
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/internal/auth/token` | No | Issue demo JWT |

### Catalog
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/internal/catalog/products` | No | List all products |
| `GET` | `/internal/catalog/products/{id}` | No | Get by ID |
| `POST` | `/internal/catalog/products` | Yes | Create product |
| `PUT` | `/internal/catalog/products/{id}` | Yes | Update product |
| `DELETE` | `/internal/catalog/products/{id}` | Yes | Delete product |

### Orders
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/internal/orders` | Yes | List all orders |
| `GET` | `/internal/orders/{id}` | Yes | Get by ID |
| `POST` | `/internal/orders` | Yes | Place an order |

### Basket
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/internal/basket/{userId}` | Yes | View cart |
| `POST` | `/internal/basket/{userId}/items` | Yes | Add item |
| `DELETE` | `/internal/basket/{userId}/items/{itemId}` | Yes | Remove item |
| `DELETE` | `/internal/basket/{userId}` | Yes | Clear cart |

---

## Project Structure

```
├── Ecommerce.API/                  # Host + Composition Root
│   ├── Program.cs                  # Service registration, middleware, seed data
│   ├── InternalControllers.cs      # Auth, Catalog, Orders, Basket controllers
│   ├── CorrelationIdMiddleware.cs
│   ├── ExceptionHandlingMiddleware.cs
│   ├── ocelot.json                 # Gateway route config
│   └── wwwroot/                    # Frontend storefront
│
├── Ecommerce.Shared/               # Cross-cutting kernel
│   ├── DomainEvent.cs              # Base event + OrderPlacedEvent
│   └── ValidationBehavior.cs       # MediatR pipeline validation
│
├── Ecommerce.Modules.Catalog/      # Product bounded context
│   ├── CatalogDbContext.cs
│   ├── CatalogHandlers.cs          # CRUD queries + commands
│   └── CatalogValidators.cs
│
├── Ecommerce.Modules.Orders/       # Order bounded context
│   ├── OrdersDbContext.cs
│   ├── CreateOrderCommand.cs       # Publishes OrderPlacedEvent
│   ├── OrderQueries.cs
│   └── CreateOrderCommandValidator.cs
│
├── Ecommerce.Modules.Basket/       # Cart bounded context
│   ├── BasketDbContext.cs
│   ├── BasketHandlers.cs           # Cart CRUD
│   └── OrderPlacedEventHandler.cs  # Reacts to order events
│
└── Ecommerce.Tests/
    └── CreateOrderCommandHandlerTests.cs
```

---

## Tech Stack

| Technology | Purpose |
|---|---|
| .NET 8 | Runtime |
| Entity Framework Core (InMemory) | Data access with schema separation |
| MediatR | CQRS + domain events |
| FluentValidation | Request validation pipeline |
| JWT Bearer | Authentication |
| Ocelot | API Gateway |
| Serilog | Structured logging |
| NUnit + Moq | Unit testing |
| Swagger | API documentation |

---

## Key Design Decisions

- **Modular Monolith over Microservices** — Same domain isolation, single deployment unit, zero network overhead. Modules can be extracted into microservices when scale demands it.
- **Schema separation** — Each module owns its database schema (`[Catalog]`, `[Orders]`, `[Basket]`), enforcing boundaries at the data level.
- **Domain events for decoupling** — Orders module publishes `OrderPlacedEvent`; Basket module subscribes and clears the cart. Neither references the other.
- **InMemory database** — For portability. Swap to `UseSqlServer()` for production with zero architectural changes.
- **Thin controllers** — All business logic lives in MediatR handlers. Controllers are just HTTP-to-CQRS translators.

---

## Running Tests

```bash
dotnet test
```

---

## License

MIT
