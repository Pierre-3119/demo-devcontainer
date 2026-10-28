using System.Text.Json;
using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Api.Endpoints;

public static class ProductEndpoints
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    private static string CacheKey(int id) => $"product:{id}";

    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/products").WithTags("Products");

        products.MapGet("/", async (AppDbContext db) =>
            await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync());

        // Lecture via Redis (cache-aside) ; l'en-tête X-Cache indique HIT ou MISS
        products.MapGet("/{id:int}", async Task<Results<Ok<Product>, NotFound>> (
            int id, AppDbContext db, IDistributedCache cache, HttpResponse response) =>
        {
            var cached = await cache.GetStringAsync(CacheKey(id));
            if (cached is not null)
            {
                response.Headers["X-Cache"] = "HIT";
                return TypedResults.Ok(JsonSerializer.Deserialize<Product>(cached)!);
            }

            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return TypedResults.NotFound();
            }

            await cache.SetStringAsync(CacheKey(id), JsonSerializer.Serialize(product), CacheOptions);
            response.Headers["X-Cache"] = "MISS";
            return TypedResults.Ok(product);
        });

        products.MapPost("/", async (ProductRequest request, AppDbContext db) =>
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                CreatedAt = DateTime.UtcNow
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/products/{product.Id}", product);
        });

        products.MapPut("/{id:int}", async Task<Results<NoContent, NotFound>> (
            int id, ProductRequest request, AppDbContext db, IDistributedCache cache) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null)
            {
                return TypedResults.NotFound();
            }

            product.Name = request.Name;
            product.Price = request.Price;
            await db.SaveChangesAsync();
            await cache.RemoveAsync(CacheKey(id));
            return TypedResults.NoContent();
        });

        products.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (
            int id, AppDbContext db, IDistributedCache cache) =>
        {
            var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync();
            if (deleted == 0)
            {
                return TypedResults.NotFound();
            }

            await cache.RemoveAsync(CacheKey(id));
            return TypedResults.NoContent();
        });
    }
}
