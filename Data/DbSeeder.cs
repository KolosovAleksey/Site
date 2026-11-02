using Microsoft.EntityFrameworkCore;
using Store.Models;

namespace Store.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var electronics = new Category { Name = "Электроника" };
        var books = new Category { Name = "Книги" };
        var home = new Category { Name = "Дом" };
        db.Categories.AddRange(electronics, books, home);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { Name = "Смартфон", Price = 29990, CategoryId = electronics.Id, ImageUrl = "https://picsum.photos/seed/1/300/200", Description = "Флагман" },
            new Product { Name = "Ноутбук", Price = 69990, CategoryId = electronics.Id, ImageUrl = "https://picsum.photos/seed/2/300/200", Description = "16 ГБ RAM" },
            new Product { Name = "Наушники", Price = 4990, CategoryId = electronics.Id, ImageUrl = "https://picsum.photos/seed/3/300/200", Description = "Беспроводные" },
            new Product { Name = "C# в действии", Price = 2500, CategoryId = books.Id, ImageUrl = "https://picsum.photos/seed/4/300/200", Description = "4-е издание" },
            new Product { Name = "ASP.NET Core", Price = 3200, CategoryId = books.Id, ImageUrl = "https://picsum.photos/seed/5/300/200", Description = "Практическое руководство" },
            new Product { Name = "Лампа", Price = 1500, CategoryId = home.Id, ImageUrl = "https://picsum.photos/seed/6/300/200", Description = "Умная лампа" }
        );
        await db.SaveChangesAsync();
    }
}