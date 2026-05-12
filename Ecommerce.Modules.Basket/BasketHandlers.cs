using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Modules.Basket;

// ── Queries ──

public record GetCartQuery(Guid UserId) : IRequest<ShoppingCart?>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, ShoppingCart?>
{
    private readonly BasketDbContext _context;
    public GetCartQueryHandler(BasketDbContext context) => _context = context;

    public async Task<ShoppingCart?> Handle(GetCartQuery request, CancellationToken ct)
        => await _context.Carts.Include(c => c.Items).AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);
}

// ── Commands ──

public record AddToCartCommand(Guid UserId, Guid ProductId, int Quantity = 1) : IRequest<Guid>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Guid>
{
    private readonly BasketDbContext _context;
    public AddToCartCommandHandler(BasketDbContext context) => _context = context;

    public async Task<Guid> Handle(AddToCartCommand request, CancellationToken ct)
    {
        var cart = await _context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null)
        {
            cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = request.UserId };
            _context.Carts.Add(cart);
        }

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }

        await _context.SaveChangesAsync(ct);
        return cart.Id;
    }
}

public record RemoveFromCartCommand(Guid UserId, Guid ItemId) : IRequest<bool>;

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, bool>
{
    private readonly BasketDbContext _context;
    public RemoveFromCartCommandHandler(BasketDbContext context) => _context = context;

    public async Task<bool> Handle(RemoveFromCartCommand request, CancellationToken ct)
    {
        var cart = await _context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        var item = cart?.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item is null) return false;

        cart!.Items.Remove(item);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}

public record ClearCartCommand(Guid UserId) : IRequest<bool>;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, bool>
{
    private readonly BasketDbContext _context;
    public ClearCartCommandHandler(BasketDbContext context) => _context = context;

    public async Task<bool> Handle(ClearCartCommand request, CancellationToken ct)
    {
        var cart = await _context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null) return false;

        cart.Items.Clear();
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
