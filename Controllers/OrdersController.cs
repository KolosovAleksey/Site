using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Data;
using Store.Models;
using Store.Services;

namespace Store.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;

    public OrdersController(AppDbContext db, ICartService cart)
    { _db = db; _cart = cart; }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var key = _cart.GetOrCreateCartKey(HttpContext);
        var cart = await _cart.GetAsync(key);
        if (cart.Lines.Count == 0) return RedirectToAction("Index", "Cart");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = new Order
        {
            UserId = userId,
            Total = cart.Total,
            Items = cart.Lines.Select(l => new OrderItem
            {
                ProductId = l.ProductId,
                ProductName = l.Name,
                UnitPrice = l.Price,
                Quantity = l.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await _cart.ClearAsync(key);

        return View("Success", order.Id);
    }
}