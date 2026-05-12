using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Modules.Catalog;

// ── Queries ──

public record GetProductsQuery : IRequest<List<Product>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<Product>>
{
    private readonly CatalogDbContext _context;
    public GetProductsQueryHandler(CatalogDbContext context) => _context = context;

    public async Task<List<Product>> Handle(GetProductsQuery request, CancellationToken ct)
        => await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
}

public record GetProductByIdQuery(Guid Id) : IRequest<Product?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Product?>
{
    private readonly CatalogDbContext _context;
    public GetProductByIdQueryHandler(CatalogDbContext context) => _context = context;

    public async Task<Product?> Handle(GetProductByIdQuery request, CancellationToken ct)
        => await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
}

// ── Commands ──

public record CreateProductCommand(string Name, string Description, decimal Price, int StockQuantity) : IRequest<Guid>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly CatalogDbContext _context;
    public CreateProductCommandHandler(CatalogDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);
        return product.Id;
    }
}

public record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, int StockQuantity) : IRequest<bool>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly CatalogDbContext _context;
    public UpdateProductCommandHandler(CatalogDbContext context) => _context = context;

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync([request.Id], ct);
        if (product is null) return false;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}

public record DeleteProductCommand(Guid Id) : IRequest<bool>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly CatalogDbContext _context;
    public DeleteProductCommandHandler(CatalogDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync([request.Id], ct);
        if (product is null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
