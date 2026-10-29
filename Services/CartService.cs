using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.Properties.Entity;

namespace WallsShop.Services;

public class CartService  
{ 
    private readonly ConcurrentDictionary<string, CachedCart> _carts = new();

    private readonly IServiceProvider _serviceProvider;
 //   private readonly IDistributedCache _cache;
    public CartService(IServiceProvider serviceProvider)//, IDistributedCache cache
    {
        _serviceProvider = serviceProvider;
        //    _cache = cache;
    }




    public async Task AddToCart(string userId, CartItem item)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WallShopContext>();

            var product = await context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Include(p => p.NewProductColors)
                .ThenInclude(p=>p.Color)
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product == null)
                return;

            // Try to find variant (optional)
            var variant = product.Variants?
                .FirstOrDefault(v => v.Id == item.VariantId);

            // If VariantId is provided but not found → error
            if (item.VariantId != 0 && variant == null)
                return;
               // throw new Exception("Variant does not exist for this product");

            // Fill cart item safely
            item.ProductName = product.Name;
            item.ImageUrl = product.Images?.FirstOrDefault()?.RelativePath ?? string.Empty;

            item.UnitPrice = variant?.Price > 0
                ? variant.Price
                : product.Price;

            item.Size = variant?.Size ?? string.Empty;
            item.Type = variant?.Type ?? string.Empty;

            var Colors= product.NewProductColors?.Where(c => c.ColorId == item.ColorId).Select(c => new { c.Color.ColorName, c.Color.EnglishColor }).ToList();
            
            item.Color = Colors.Select(c => c.ColorName).FirstOrDefault() ?? item.Color ?? string.Empty;
            item.EnglishColor = Colors.Select(c => c.EnglishColor).FirstOrDefault() ?? item.EnglishColor ?? string.Empty;
            // البحث في Cart
            var cachedCart = _carts.GetOrAdd(userId, _ => new CachedCart());
          
            var existingItem = cachedCart.Cart.Items
                .FirstOrDefault(i =>
                    i.ProductId == item.ProductId &&
                    i.VariantId == item.VariantId &&
                      (i.ColorId==item.ColorId));
          
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                cachedCart.Cart.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            return; //throw new Exception($"Error adding to cart: {ex.Message}", ex);
        }
    }


    public async Task<List<CartItem>> GetCartItems(string userId, string languageCode="en")
    {
        if (_carts.TryGetValue(userId, out var cached))
        {

         var items = await GetProductImageName(cached.Cart.Items, languageCode);
            return items;


        }

        return new List<CartItem>();
    }
    public async Task<int> GetCartItemsCount(string userId, string languageCode="en")
    {
        if (_carts.TryGetValue(userId, out var cached))
        {

         var items = await GetProductImageNameCount(cached.Cart.Items, languageCode);
            return items;


        }

        return 0;
    }
    public async Task<List<CartItem>> GetProductImageName(List<CartItem> items, string languageCode)
    {
        List<CartItem> returnList = new();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WallShopContext>();

        bool isArabic = languageCode.Equals("ar", StringComparison.OrdinalIgnoreCase);

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.NameEN,
                p.Price,
                p.PriceAfterDiscount,

                Image = p.Images
                    .Select(i => i.RelativePath)
                    .FirstOrDefault(),

                Variants = p.Variants.Select(v => new
                {
                    v.Id,
                    v.Size,
                    v.Type,
                    v.EnglishSize,
                    v.EnglishType,
                    v.Price,
                    v.PriceBeforeDiscount
                }).ToList(),

                Colors = p.NewProductColors.Select(c => new
                {
                    c.ColorId,
                    c.Color.ColorName,
                    c.Color.EnglishColor
                }).ToList()
            })
            .AsNoTracking()
            .ToListAsync();

        foreach (var item in items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);

            if (product == null)
                continue;

            var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);

            if (item.VariantId > 0 && variant == null)
                continue;

            bool existVariant = variant != null;
            if (existVariant && item.VariantId == 0)
                continue;

            var color = product.Colors.FirstOrDefault(c => c.ColorId == item.ColorId);

            string colorAR = color?.ColorName ?? "";
            string colorEN = color?.EnglishColor ?? "";

            var cartItem = new CartItem
            {
                ProductId = item.ProductId,
                ColorId = item.ColorId,
                VariantId = item.VariantId,
                Quantity = item.Quantity,

                ProductName = isArabic ? product.Name : product.NameEN,

                Color = isArabic ? colorAR : colorEN,
                EnglishColor = colorEN,

                Size = isArabic ? variant?.Size ?? "" : variant?.EnglishSize ?? "",
                Type = isArabic ? variant?.Type ?? "" : variant?.EnglishType ?? "",

                UnitPrice = item.VariantId > 0
                    ? (variant?.Price ?? product.PriceAfterDiscount)
                    : product.PriceAfterDiscount,

                OriginalPrice = item.VariantId > 0
                    ? (variant?.PriceBeforeDiscount ?? variant?.Price ?? product.Price)
                    : product.Price,

                ImageUrl = product.Image ?? string.Empty
            };

            returnList.Add(cartItem);
        }

        return returnList;
    }

    public async Task<int> GetProductImageNameCount(List<CartItem> items, string languageCode)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WallShopContext>();

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                VariantIds = p.Variants.Select(v => v.Id).ToList()
            })
            .AsNoTracking()
            .ToListAsync();

        HashSet<(int ProductId, int VariantId)> uniqueItems = new();

        foreach (var item in items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);

            if (product == null)
                continue;

            bool existVariant = product.VariantIds.Contains(item.VariantId);

            if ((existVariant && item.VariantId == 0) || (!existVariant && item.VariantId != 0))
                continue;

            uniqueItems.Add((item.ProductId, item.VariantId));
        }

        return uniqueItems.Count;
    }
 
    public void CleanupExpiredCarts()
    {
        var expirationTime = DateTime.UtcNow.AddHours(-48);
        var expiredKeys = _carts.Where(kvp => kvp.Value.CreatedAt < expirationTime)
            .Select(kvp => kvp.Key);

        foreach (var key in expiredKeys)
        {
            _carts.TryRemove(key, out _);
        }
    }
    // ضيف الدالة دي جوه CartService.cs
    public void ClearCart(string userId)
    {
        // TryRemove بتحذف العنصر من الـ Dictionary بأمان
        _carts.TryRemove(userId, out _);
    }

    // داخل CartService.cs

    public async Task<List<CartItem>?> UpdateQuantityAsync(string userId, int productId, int variantId, int quantity, int colorId, string languageCode)
    {
      
        if (_carts.TryGetValue(userId, out var cachedCart))
        {
           
            var itemToUpdate = cachedCart.Cart.Items
                .FirstOrDefault(i => i.ProductId == productId &&
                                     i.VariantId == variantId &&
                                     i.ColorId == colorId);

            if (itemToUpdate != null)
            {
                if (quantity <= 0)
                {
                    cachedCart.Cart.Items.Remove(itemToUpdate);
                }
                else
                {
                    itemToUpdate.Quantity = quantity;
                }

                return await GetCartItems(userId, languageCode);
            }
        }

        return null;
    }
    // داخل CartService.cs

    public async Task<List<CartItem>?> RemoveItemAsync(string userId, int productId, int variantId, int colorId, string languageCode)
    {
        if (_carts.TryGetValue(userId, out var cachedCart))
        {
            var itemToDelete = cachedCart.Cart.Items
                .FirstOrDefault(i => i.ProductId == productId &&
                                     i.VariantId == variantId && i.ColorId == colorId);
            //i.Color == (color ?? ""));

            if (itemToDelete != null)
            {
                cachedCart.Cart.Items.Remove(itemToDelete);

                return await GetCartItems(userId, languageCode);
            }
        }

        return null;
    }

    public int GetCountOfCartsForSpecificUser(string userId)
    {
        if (_carts.TryGetValue(userId, out var cached))
        {

            int CartsCount = cached.Cart.Items.Count;
            return CartsCount;


        }

        return 0;
    }
}