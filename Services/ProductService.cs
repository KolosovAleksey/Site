using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Store.Data;
using Store.DTOs;

namespace Store.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetCatalogAsync(int? categoryId, string? search, int page, int pageSize);
    Task<List<Models.Category>> GetCategoriesAsync();
    Task InvalidateCatalogAsync();
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductService> _logger;
    private static int _cacheVersion = 0;   // В production — Redis-счётчик

    public ProductService(AppDbContext db, IMemoryCache cache, ILogger<ProductService> logger)
    {
        _db = db; _cache = cache; _logger = logger;
    }

    private string VKey(string k) => $"v{Volatile.Read(ref _cacheVersion)}:{k}";

    public Task<List<ProductDto>> GetCatalogAsync(int? categoryId, string? search, int page, int pageSize)
    {
        var key = VKey($"catalog:{categoryId}:{search}:{page}:{pageSize}");
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            entry.Size = 1;
            _logger.LogInformation("Cache MISS {Key}", key);

            var q = _db.Products.AsNoTracking().AsQueryable();
            if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));

            return await q
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(
                    p.Id, p.Name, p.Price, p.ImageUrl,
                    p.CategoryId, p.Category!.Name))
                .ToListAsync();
        })!;
    }

    public Task<List<Models.Category>> GetCategoriesAsync() =>
        _cache.GetOrCreateAsync(VKey("categories"), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _db.Categories.AsNoTracking().ToListAsync();
        })!;

    public Task InvalidateCatalogAsync()
    {
        Interlocked.Increment(ref _cacheVersion);
        _logger.LogWarning("Catalog cache invalidated. New version: {Ver}", _cacheVersion);
        return Task.CompletedTask;
    }
}