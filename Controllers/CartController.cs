using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Store.Services;

namespace Store.Controllers;

[AutoValidateAntiforgeryToken]  // Проверяет antiforgery для всех POST/DELETE
public class CartController : Controller
{
    private readonly ICartService _cart;
    public CartController(ICartService cart) => _cart = cart;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var key = _cart.GetOrCreateCartKey(HttpContext);
        return View(await _cart.GetAsync(key));
    }

    [HttpGet]
    public async Task<IActionResult> Summary()
    {
        var key = _cart.GetOrCreateCartKey(HttpContext);
        return PartialView("_CartSummary", await _cart.GetAsync(key));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var key = _cart.GetOrCreateCartKey(HttpContext);
        return Json(new { count = await _cart.CountAsync(key) });
    }

    [HttpPost]
    [EnableRateLimiting("ajax")]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var key = _cart.GetOrCreateCartKey(HttpContext);
        await _cart.AddAsync(key, productId, quantity);
        if (IsAjax()) return PartialView("_CartSummary", await _cart.GetAsync(key));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [EnableRateLimiting("ajax")]
    public async Task<IActionResult> Remove(int id)
    {
        var key = _cart.GetOrCreateCartKey(HttpContext);
        await _cart.RemoveAsync(key, id);
        if (IsAjax()) return PartialView("_CartSummary", await _cart.GetAsync(key));
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjax() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest";
}