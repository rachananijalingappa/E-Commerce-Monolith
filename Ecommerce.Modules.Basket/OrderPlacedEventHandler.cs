using Ecommerce.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Modules.Basket;

/// <summary>
/// Reacts to OrderPlacedEvent by clearing the user's shopping cart.
/// This demonstrates cross-module communication via domain events.
/// </summary>
public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly BasketDbContext _context;

    public OrderPlacedEventHandler(BasketDbContext context) => _context = context;

    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == notification.UserId, cancellationToken);

        if (cart is null) return;

        cart.Items.Clear();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
