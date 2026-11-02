using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Store.Models;

namespace Store.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public AccountController(UserManager<ApplicationUser> users,
                             SignInManager<ApplicationUser> signIn)
    { _users = users; _signIn = signIn; }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError("", "Неверный email или пароль");
            return View();
        }
        var res = await _signIn.PasswordSignInAsync(user, password, true, true);
        if (!res.Succeeded)
        {
            ModelState.AddModelError("", "Неверный email или пароль");
            return View();
        }
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        var res = await _users.CreateAsync(user, password);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
            return View();
        }
        await _signIn.SignInAsync(user, true);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}