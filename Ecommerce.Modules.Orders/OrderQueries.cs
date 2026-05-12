using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Modules.Orders;

public record GetOrdersQuery : IRequest<List<Order>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<Order>>
{
    private readonly OrdersDbContext _context;
    public GetOrdersQueryHandler(OrdersDbContext context) => _context = context;

    public async Task<List<Order>> Handle(GetOrdersQuery request, CancellationToken ct)
        => await _context.Orders.Include(o => o.Items).AsNoTracking()
            .OrderByDescending(o => o.OrderDate).ToListAsync(ct);
}

public record GetOrderByIdQuery(Guid Id) : IRequest<Order?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Order?>
{
    private readonly OrdersDbContext _context;
    public GetOrderByIdQueryHandler(OrdersDbContext context) => _context = context;

    public async Task<Order?> Handle(GetOrderByIdQuery request, CancellationToken ct)
        => await _context.Orders.Include(o => o.Items).AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);
}
