using MediatR;
using Ecommerce.Shared;

namespace Ecommerce.Modules.Orders;

public record CreateOrderCommand(Guid UserId, List<Guid> ProductIds) : IRequest<Guid>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly OrdersDbContext _context;
    private readonly IMediator _mediator;

    public CreateOrderCommandHandler(OrdersDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 99.99m
        };

        foreach (var productId in request.ProductIds)
        {
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Quantity = 1
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // Notify other modules (e.g. Basket clears the user's cart)
        await _mediator.Publish(new OrderPlacedEvent(order.UserId, order.Id), cancellationToken);

        return order.Id;
    }
}
