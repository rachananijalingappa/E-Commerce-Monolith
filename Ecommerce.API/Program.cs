using Microsoft.EntityFrameworkCore;
using Ecommerce.Modules.Catalog;
using Ecommerce.Modules.Basket;
using Ecommerce.Modules.Orders;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;
using Serilog;
using Ecommerce.Shared;
using FluentValidation;
using MediatR;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// EF Core — using InMemory provider for portability (swap to UseSqlServer for production)
builder.Services.AddDbContext<CatalogDbContext>(o => o.UseInMemoryDatabase("EcommerceDb"));
builder.Services.AddDbContext<BasketDbContext>(o => o.UseInMemoryDatabase("EcommerceDb"));
builder.Services.AddDbContext<OrdersDbContext>(o => o.UseInMemoryDatabase("EcommerceDb"));

// MediatR + FluentValidation pipeline
builder.Services.AddValidatorsFromAssembly(typeof(GetProductsQuery).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(OrderPlacedEventHandler).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderCommand).Assembly);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetProductsQuery).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(OrderPlacedEventHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// JWT
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyThatIsAtLeast32BytesLong!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "EcommerceAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "EcommerceAPI";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// Gateway, Swagger, Health
builder.Services.AddOcelot();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Hide Ocelot's internal admin endpoints from the Swagger UI
    c.DocInclusionPredicate((_, apiDesc) =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"] ?? "";
        return controller is not ("FileConfiguration" or "OutputCache");
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseMiddleware<Ecommerce.API.ExceptionHandlingMiddleware>();
app.UseMiddleware<Ecommerce.API.CorrelationIdMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = $"{report.TotalDuration.TotalMilliseconds:F1}ms"
        }));
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed the in-memory database on startup
using (var scope = app.Services.CreateScope())
{
    var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var basketDb = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
    var ordersDb = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

    catalogDb.Database.EnsureCreated();
    basketDb.Database.EnsureCreated();
    ordersDb.Database.EnsureCreated();

    if (!catalogDb.Products.Any())
    {
        catalogDb.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Wireless Headphones", Description = "Noise-cancelling over-ear headphones with 30hr battery", Price = 129.99m, StockQuantity = 50 },
            new Product { Id = Guid.NewGuid(), Name = "Mechanical Keyboard", Description = "RGB backlit keyboard with Cherry MX switches", Price = 89.95m, StockQuantity = 120 },
            new Product { Id = Guid.NewGuid(), Name = "4K Monitor", Description = "27-inch IPS display with HDR support", Price = 449.00m, StockQuantity = 30 },
            new Product { Id = Guid.NewGuid(), Name = "USB-C Hub", Description = "7-in-1 hub with HDMI, USB 3.0, and SD card reader", Price = 34.99m, StockQuantity = 200 },
            new Product { Id = Guid.NewGuid(), Name = "Ergonomic Mouse", Description = "Vertical wireless mouse for reduced wrist strain", Price = 59.50m, StockQuantity = 80 },
            new Product { Id = Guid.NewGuid(), Name = "Laptop Stand", Description = "Adjustable aluminium stand with cable management", Price = 42.00m, StockQuantity = 95 }
        );
        catalogDb.SaveChanges();
    }
}

// Ocelot handles /api/* routes; internal controllers serve everything else directly
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), branch =>
{
    branch.UseOcelot().Wait();
});

app.Run();
