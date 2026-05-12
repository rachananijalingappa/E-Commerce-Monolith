using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MediatR;
using Ecommerce.Modules.Catalog;
using Ecommerce.Modules.Orders;
using Ecommerce.Modules.Basket;

namespace Ecommerce.API.Controllers;

// ── Auth ──

[ApiController]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    public AuthController(IConfiguration config) => _config = config;

    /// <summary>Issues a short-lived JWT for demo/testing purposes.</summary>
    [AllowAnonymous]
    [HttpGet("internal/auth/token")]
    public IActionResult GenerateToken()
    {
        var secret = _config["JwtSettings:SecretKey"] ?? "SuperSecretKeyThatIsAtLeast32BytesLong!";
        var issuer = _config["JwtSettings:Issuer"] ?? "EcommerceAPI";
        var audience = _config["JwtSettings:Audience"] ?? "EcommerceAPI";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer, audience: audience,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

// ── Catalog ──

[ApiController]
[Tags("Catalog")]
[Route("internal/catalog/products")]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    public CatalogController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetProductsQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id));
        return product is not null ? Ok(product) : NotFound();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id) return BadRequest("Route ID and body ID must match.");
        var updated = await _mediator.Send(command);
        return updated ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteProductCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}

// ── Orders ──

[ApiController]
[Authorize]
[Tags("Orders")]
[Route("internal/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetOrdersQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        return order is not null ? Ok(order) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] List<Guid> productIds)
    {
        // TODO: extract UserId from JWT claims in a production scenario
        var userId = Guid.NewGuid();
        var orderId = await _mediator.Send(new CreateOrderCommand(userId, productIds));
        return CreatedAtAction(nameof(GetById), new { id = orderId }, new { orderId });
    }
}

// ── Basket ──

[ApiController]
[Authorize]
[Tags("Basket")]
[Route("internal/basket")]
public class BasketController : ControllerBase
{
    private readonly IMediator _mediator;
    public BasketController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetCart(Guid userId)
    {
        var cart = await _mediator.Send(new GetCartQuery(userId));
        return cart is not null ? Ok(cart) : Ok(new { userId, items = Array.Empty<object>() });
    }

    [HttpPost("{userId:guid}/items")]
    public async Task<IActionResult> AddItem(Guid userId, [FromBody] AddToCartRequest request)
    {
        var cartId = await _mediator.Send(new AddToCartCommand(userId, request.ProductId, request.Quantity));
        return Ok(new { cartId });
    }

    [HttpDelete("{userId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid userId, Guid itemId)
    {
        var removed = await _mediator.Send(new RemoveFromCartCommand(userId, itemId));
        return removed ? NoContent() : NotFound();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> ClearCart(Guid userId)
    {
        var cleared = await _mediator.Send(new ClearCartCommand(userId));
        return cleared ? NoContent() : NotFound();
    }
}

public record AddToCartRequest(Guid ProductId, int Quantity = 1);
