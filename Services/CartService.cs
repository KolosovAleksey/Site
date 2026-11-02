using Microsoft.EntityFrameworkCore;
using Store.Data;
using Store.DTOs;
using Store.Models;

namespace Store.Services;

public interface ICartService
{
    string GetOrCreateCartKey(HttpContext ctx);
    Task AddAsync(string cartKey, int productId, int qty);
    Task RemoveAsync(string cartKey, int cartItemId);
    Task<CartDto> GetAsync(string cartKey);
    Task<int> CountAsync(string cartKey);
    Task ClearAsync(string cartKey);
}

public class CartService : ICartService
{
    private const string CartKeyName = "CartKey";
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public string GetOrCreateCartKey(HttpContext ctx)
    {
        var key = ctx.Session.GetString(CartKeyName);
        if (string.IsNullOrEmpty(key))
        {
            key = Guid.NewGuid().ToString("N");
            ctx.Session.SetString(CartKeyName, key);
        }
        return key;
    }

    public async Task AddAsync(string cartKey, int productId, int qty)
    {
        if (qty < 1) qty = 1;
        if (!await _db.Products.AnyAsync(p => p.Id == productId)) return;

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.CartKey == cartKey && c.ProductId == productId);

        if (existing != null)
            existing.Quantity += qty;
        else
            _db.CartItems.Add(new CartItem { CartKey = cartKey, ProductId = productId, Quantity = qty });

        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(string cartKey, int cartItemId)
    {
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.CartKey == cartKey);
        if (item == null) return;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task<CartDto> GetAsync(string cartKey)
    {
        var lines = await _db.CartItems.AsNoTracking()
            .Where(c => c.CartKey == cartKey)
            .Select(c => new CartLineDto(
                c.Id, c.ProductId, c.Product!.Name, c.Product.Price,
                c.Quantity, c.Product.Price * c.Quantity))
            .ToListAsync();

        return new CartDto(lines, lines.Sum(l => l.LineTotal));
    }

    public async Task<int> CountAsync(string cartKey) =>
        await _db.CartItems.AsNoTracking()
            .Where(c => c.CartKey == cartKey)
            .SumAsync(c => (int?)c.Quantity) ?? 0;

    public async Task ClearAsync(string cartKey)
    {
        var items = await _db.CartItems.Where(c => c.CartKey == cartKey).ToListAsync();
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
    }
}