# E-Commerce Modular Monolith Walkthrough

Welcome to the finished E-Commerce architecture! We built a pristine, cloud-native `.NET 8` portfolio project designed to showcase decoupled domains without the infrastructure overhead of microservices. 

> [!TIP]
> **Zero Installation Required:** We specifically architected this portfolio piece to use the Entity Framework **In-Memory Database**. Any recruiter or engineer pulling your GitHub repository can press F5 or run `dotnet run` instantly—no Docker or SQL Server installation required on their machine!

## How It Works
1.  **Architecture:** The solution has an `Ecommerce.API` host project and four distinct, decoupled core libraries (`Catalog`, `Basket`, `Orders`, and `Shared`).
2.  **Entity Framework Core:** We isolated our contexts into three instances (`CatalogDbContext`, `BasketDbContext`, and `OrdersDbContext`), perfectly segregating domain models.
3.  **Strict CQRS Implementation:** Business logic is fully encapsulated behind `CreateOrderCommand` and executed cleanly by MediatR handlers. Controllers are completely blind to the actual storage processing.
4.  **Cross-Module Domain Events:** Domains do not have circular references. For example, when an order is finalized, MediatR dispatches an `OrderPlacedEvent` into the internal message bus. The `Basket` module listens, catches the event, and silently wipes the user's shopping cart clean.
5.  **API Gateway & Security:** The system uses Ocelot to intelligently route the `/api/v1/*` paths down to internally protected HTTP endpoints securely locked by `JwtBearer` authentication.
6.  **Observability & Tracing:** Centralized structured logging is actively configured utilizing **Serilog**. Every single request passes through a custom `CorrelationIdMiddleware` that traps or automatically generates an `X-Correlation-ID`. This ensures that logs across every decoupled bounded context are globally traceable.

## Local Execution Instructions
1. Navigate to the `Ecommerce.API` folder:
```bash
cd Ecommerce.API
```
2. Run the .NET API:
```bash
dotnet run
```
3. Open your browser and navigate to:
```url
http://localhost:5000/swagger
```
The interactive API dashboard will automatically load, giving you full access to test endpoints and validate the enterprise data flow.
