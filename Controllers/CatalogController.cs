using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Store.Services;

namespace Store.Controllers;

public class CatalogController : Controller
{
    private readonly IProductService _products;
    public CatalogController(IProductService products) => _products = products;

    public async Task<IActionResult> Index(int? categoryId, string? search, int page = 1)
    {
        ViewBag.Categories = await _products.GetCategoriesAsync();
        ViewBag.CategoryId = categoryId;
        ViewBag.Search = search;
        ViewBag.Page = page;
        var items = await _products.GetCatalogAsync(categoryId, search, page, 12);
        return View(items);
    }

    // Отдаёт ТОЛЬКО сетку товаров, кэшируется сервером на 30 сек.
    [OutputCache(PolicyName = "catalog")]
    public async Task<IActionResult> Grid(int? categoryId, string? search, int page = 1)
    {
        var items = await _products.GetCatalogAsync(categoryId, search, page, 12);
        return PartialView("_ProductGrid", items);
    }
}