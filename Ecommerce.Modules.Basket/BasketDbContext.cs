using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Modules.Basket;

public class ShoppingCart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class BasketDbContext : DbContext
{
    public BasketDbContext(DbContextOptions<BasketDbContext> options) : base(options) { }

    public DbSet<ShoppingCart> Carts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Basket");
        modelBuilder.Entity<ShoppingCart>().ToTable("Carts");
        modelBuilder.Entity<ShoppingCart>().HasKey(c => c.Id);
        modelBuilder.Entity<CartItem>().ToTable("CartItems");
        modelBuilder.Entity<CartItem>().HasKey(i => i.Id);
    }
}
