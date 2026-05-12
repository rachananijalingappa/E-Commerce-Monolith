using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Ecommerce.Modules.Orders;
using Ecommerce.Shared;

namespace Ecommerce.Tests;

[TestFixture]
public class CreateOrderCommandHandlerTests
{
    private OrdersDbContext _context = null!;
    private Mock<IMediator> _mediatorMock = null!;
    private CreateOrderCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase($"OrdersTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new OrdersDbContext(options);
        _mediatorMock = new Mock<IMediator>();
        _handler = new CreateOrderCommandHandler(_context, _mediatorMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task Handle_ShouldPersistOrder_AndPublishDomainEvent()
    {
        var userId = Guid.NewGuid();
        var productIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new CreateOrderCommand(userId, productIds);

        var orderId = await _handler.Handle(command, CancellationToken.None);

        var savedOrder = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.That(savedOrder, Is.Not.Null);
        Assert.That(savedOrder!.UserId, Is.EqualTo(userId));
        Assert.That(savedOrder.Items, Has.Count.EqualTo(2));

        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<OrderPlacedEvent>(e => e.UserId == userId && e.OrderId == orderId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
