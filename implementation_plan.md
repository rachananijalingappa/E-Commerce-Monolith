# E-Commerce Modular Monolith Implementation Plan

This is the executed architecture plan for the `.NET 8` Enterprise E-Commerce project. The core focus is to prove mastery of modern system architecture patterns by splitting monolithic spaghetti code into cleanly separated bounded contexts (Modules) before attempting full Microservices.

## Architectural Decisions
*   **Decoupled Bounded Contexts:** We will build `Catalog`, `Basket`, and `Orders` as entirely separated class libraries that map to individual business domains.
*   **Database Philosophy:** The SQL layer will utilize Entity Framework Core. To make this extremely viable as a fast-cloning GitHub portfolio piece, we will bind the databases to an EF Core **InMemoryDatabase**, effectively removing the need for reviewers to pull docker containers or configure connection strings. 
*   **Communications:** Strictly cross-module via `MediatR` Domain Events to assure they never form direct hardboard dependencies.
*   **Testing Setup:** Implement NUnit and Moq to generate an isolated, programmatic sandbox validating all our queries and commands.
*   **Observability:** Implement robust structured logging natively utilizing **Serilog**. Emits and binds a universal `X-Correlation-ID` header directly into the execution `LogContext` using ASP.NET Core Middleware.

## Component Breakdown

### 1. `Ecommerce.Shared` [Core Infrastructure]
*   **Purpose:** Houses cross-cutting abstractions.
*   **Keys:** `DomainEvent.cs` which inherits from `INotification` so modules can natively pass messages (like `OrderPlacedEvent`) to each other via MediatR without relying on RabbitMQ.

### 2. `Ecommerce.Modules.Catalog` [Domain]
*   **Purpose:** The single source of truth for products and pricing.
*   **Data Layer:** `CatalogDbContext`
*   **Operations:** `GetProductsQuery` MediatR Query handler returning dynamic `Product` listings.

### 3. `Ecommerce.Modules.Basket` [Domain]
*   **Purpose:** Temporary user shopping carts.
*   **Data Layer:** `BasketDbContext`
*   **Operations:** Implements an `OrderPlacedEventHandler` (an `INotificationHandler`) that actively listens to the system bus and immediately clears a user's items out when it detects a completed sale.

### 4. `Ecommerce.Modules.Orders` [Domain]
*   **Purpose:** Finalized transactional ledgers.
*   **Data Layer:** `OrdersDbContext`
*   **Operations:** Centralized `CreateOrderCommand` logic which saves the order rows and then publishes the necessary event out to the rest of the monolith to close the business loop.

### 5. `Ecommerce.API` [Gateway Host]
*   **Purpose:** Central HTTP listener.
*   **Authentication:** Sets up `JWT Bearer` Token Validation.
*   **Routing:** Configures `Ocelot.json` to proxy and redirect external internet traffic dynamically into the internal domain logic.
*   **Documentation:** Fully configured Swagger/Swashbuckle UI to elegantly demonstrate the working build.
*   **Logging:** Bootstraps Serilog for structured request tracking, logging runtime processes directly against their dynamic API scopes.

### 6. `Ecommerce.Tests` [Quality Assurance]
*   **Purpose:** Ensures mathematical confidence in the business tier.
*   **Tests Built:** End-to-end `CreateOrderCommandHandlerTests` dynamically instantiating another InMemory DB and utilizing `Mock<IMediator>` to confirm our cross-module events emit flawlessly on command.
