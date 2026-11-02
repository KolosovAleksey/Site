using Microsoft.AspNetCore.Mvc;

namespace Store.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    [ResponseCache(Duration = 0)] public IActionResult Error() => Content("Ошибка");
}