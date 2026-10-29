using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.Entity;
using WallsShop.Extensions;
using WallsShop.Properties.Entity;
 

namespace WallsShop.Repository;

public class ProductRepository(WallShopContext ctx)
{

    public async Task<PagedResult<ProductResponseDto>> GetArabicProductById(QueryParameters queryParameters, string? userId = null)
    {
        var query = ctx.Products.AsNoTracking().AsQueryable();
        query.Include(c => c.CategoryImage);
        query.Include(c => c.NewProductColors).ThenInclude(c=>c.Color);
      
        if (queryParameters.search != null)
        {

            query = query.Where(a => a.Name.Contains(queryParameters.search) || a.Descriptions.Contains(queryParameters.search));



        }
        string categoryNameDisplay = "All Categories";
        //if (!string.IsNullOrEmpty(queryParameters.category))
        //{
        //    var cat = queryParameters.category.ToLower();
        //    query = query.Where(a => a.Category.ToLower() == cat);
        //    categoryNameDisplay = queryParameters.category;
        //}
        if (!string.IsNullOrEmpty(queryParameters.category))
        {
            var cat = queryParameters.category.ToLower();
            query = query.Where(a => a.CategoryImage.CategoryValue.ToLower() == cat);
            var category = await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
            if (category != null)
            {
                categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

            }
            else
            {
                categoryNameDisplay = queryParameters.category;
            }
        }
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;

        //////////////
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);
        /////////////////////


        if (queryParameters.id > 0)
        {
            query = query.Where(a => a.Id == queryParameters.id);
        }
        //var totalCount = await query.CountAsync();

        //var pageSize = queryParameters.pageSize > 0 ? queryParameters.pageSize : 10;
        //var currentPage = queryParameters.page > 0 ? queryParameters.page : 1;

        //var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        query = (queryParameters.order ?? "").ToLower() switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "rating_desc" => query.OrderByDescending(a => a.AverageRate),
            "rating_asc" => query.OrderBy(a => a.AverageRate),
            "latest_asc" => query.OrderBy(a => a.Created),
            "latest_desc" => query.OrderByDescending(a => a.Created),
            _ => query.OrderBy(a => a.Id) // Default ordering is good practice for pagination
        };


        //if (queryParameters.page > 0 && queryParameters.pageSize > 0)
        //{
        //    var skip = (queryParameters.page - 1) * queryParameters.pageSize;
        //    query = query.Skip(skip).Take(queryParameters.pageSize);
        //}


        //return await query.Select(p => new ProductResponseDto
        //{
        //    Id = p.Id,
        //    Name = p.Name,
        //    Price = p.Price,
        //    PriceAfterDiscount = p.PriceAfterDiscount,
        //    Description = p.Descriptions,
        //    ShortDescription = p.FullDescription,
        //    Category = p.Category.Replace("-"," "),
        //    CateogryValue = p.CategoryValue,
        //    SKU = p.SKU,
        //    IsInWishList = wishlistIds.Contains(p.Id),
        //    Images = p.Images.Select(img => new ProductImageDto
        //    {
        //        Path = img.RelativePath
        //    }).ToList(),
        //    Colors = p.Colors.Where(a=>a.LanguageCode == "ar").Select(c => c.Color).ToList(),
        //    Variants = p.Variants
        //        .Where(a=>a.LanguageCode == "ar")
        //        .Select(v => new ProductVariantDto
        //    {
        //        Id = v.Id,
        //        Type = v.Type,
        //        Size = v.Size,
        //        Price = v.Price,
        //        PriceBeforeDiscount = v.PriceBeforeDiscount ?? 0
        //    }).ToList()
        //}).ToListAsync();



        var dtoQuery = query.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            PriceAfterDiscount = p.PriceAfterDiscount,
            //   Description = p.Descriptions.Replace("\r\n", "\n"),
            Descriptions = p.Descriptions
    .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
    .ToList(),

            AverageRate = p.AverageRate,


            ShortDescription = p.FullDescription,
            Category = p.CategoryImage.Category.Replace("-", " "),
            CateogryValue = p.CategoryImage.CategoryValue,
            SKU = p.SKU,

            PageNumber = currentPage,

            IsInWishList = wishlistIds.Contains(p.Id),
            Images = p.Images.Select(img => new ProductImageDto { Path = img.RelativePath }).ToList(),
            // Colors = p.Colors.Where(a => a.LanguageCode == "ar").Select(c => c.Color).ToList(),
            //  Colors = p.NewProductColors.Where(c => c.ProductId == p.Id).Select(c => c.Color.ColorName).ToList(),
            Colors = p.NewProductColors
    .Where(c => c.ProductId == p.Id)
    .Select(c => new ColorDto
    {
        ColorId = c.Color.Id,
        ColorName = c.Color.ColorName
    })
    .ToList(),


            Variants = p.Variants.Where(a => a.LanguageCode == "ar")
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Type = v.Type,
                    Size = v.Size,
                    Price = v.Price,
                    PriceBeforeDiscount = v.PriceBeforeDiscount ?? 0
                }).ToList()
        });

        return await dtoQuery.ToPagedListAsync(
            queryParameters.page,
            queryParameters.pageSize,
            categoryNameDisplay
        );
    }

    public async Task<PagedResult<ProductTranslationDto>> GetEnglishProductById(QueryParameters queryParameters, string? userId = null)
    {

        //var query = ctx
        //    .ProductTranslations
        //    .Include(t => t.Product)
        //    .ThenInclude(p => p.NewProductColors)
        //    .ThenInclude(c => c.Color)
        //    .Include(t => t.Product)
        //    .ThenInclude(p => p.Variants)
        //    .Include(t => t.Product)
        //    .ThenInclude(t => t.Images)
        //    .Include(t => t.Product).ThenInclude(c => c.CategoryImage)
        //    .AsQueryable();
        var query = ctx.Products.AsNoTracking().AsQueryable();
        query= query.Include(c => c.CategoryImage);
        query= query.Include(c => c.NewProductColors).ThenInclude(c => c.Color);
        if (queryParameters.search != null)
        {

            var translatedProductIds = await ctx.Products
                .Where(t => t.Name.Contains(queryParameters.search) || t.DescriptionsEN.Contains(queryParameters.search))
                .Select(t => t.Id)
                .ToListAsync();
            query = query.Where(a => translatedProductIds.Contains(a.Id));


        }
        string categoryNameDisplay = "";
        if (queryParameters.LanguageCode == "en")
            categoryNameDisplay = "All Categories";
        else
            categoryNameDisplay = "كل الاقسام";
        //if (!string.IsNullOrEmpty(queryParameters.category))
        //{
        //    var cat = queryParameters.category.ToLower();
        //    query = query.Where(a => a.Category.ToLower() == cat);
        //    categoryNameDisplay = queryParameters.category;
        //}
        if (!string.IsNullOrEmpty(queryParameters.category))
        {
            var cat = queryParameters.category.ToLower();
            query = query.Where(a => a.CategoryImage.Category.ToLower() == cat);
            var category = await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
            if (category != null)
            {
                categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

            }
            else
            {
                categoryNameDisplay = queryParameters.category;
            }
        }
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        if (queryParameters.id > 0)
        {
            query = query.Where(a => a.Id == queryParameters.id);
        }

        //if (!string.IsNullOrEmpty(queryParameters.category))
        //{
        //    query = query.Where(a => a.Category.ToLower() == queryParameters.category.ToLower());
        //}

        //if (queryParameters.page > 0 && queryParameters.pageSize > 0)
        //{
        //    var skip = (queryParameters.page - 1) * queryParameters.pageSize;
        //    query = query.Skip(skip).Take(queryParameters.pageSize);
        //}


        query = queryParameters.order.ToLower() switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "rating_desc" => query.OrderByDescending(a => a.AverageRate),
            "rating_asc" => query.OrderBy(a => a.AverageRate),
            "latest_asc" => query.OrderBy(a => a.Created),
            "latest_desc" => query.OrderByDescending(a => a.Created),
            _ => query
        };
        var dtoQuery = query.Select(pt => new ProductTranslationDto
        {
            Id = pt.Id,
            // ProductId = pt.ProductId,
            Name = pt.NameEN,
            ShortDescription = pt.ShortDescriptionEn,//.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)[0],
            Descriptions = pt.DescriptionsEN.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList(),
            Category = pt.CategoryImage.CategoryValue.Replace("-", " "),
            CateogryValue = pt.CategoryImage.CategoryValue,
            AverageRate = pt.AverageRate,

            PageNumber = currentPage,

            //Variants = pt.Product.Variants.Where(a => a.LanguageCode == "ar").ToList(),
            Variants = pt.Variants.Where(a => a.LanguageCode == "ar")
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Type = v.EnglishType,
                    Size = v.EnglishSize,
                    Price = v.Price,
                    PriceBeforeDiscount = v.PriceBeforeDiscount ?? 0
                }).ToList(),

            Price = pt.Price,
            PriceAfterDiscount = pt.PriceAfterDiscount,
            SKU = pt.SKU,
            IsInWishList = wishlistIds.Contains(pt.Id),
            Images = pt.Images.Select(img => new ProductImageDto()
            {
                Path = img.RelativePath
            }).ToList(),
            //  Colors = pt.Product.Colors.Where(a => a.LanguageCode == "en").Select(c => c.Color).ToList()
            //  Colors = pt.Product.NewProductColors.Where(p=>p.ProductId==pt.Id).Select(c => c.Color.EnglishColor).ToList()
            Colors = pt.NewProductColors
    .Where(c => c.ProductId == pt.Id)
    .Select(c => new ColorDto
    {
        ColorId = c.Color.Id,
        ColorName = c.Color.EnglishColor
    })
    .ToList(),

        });

        return await dtoQuery.ToPagedListAsync(queryParameters.page, queryParameters.pageSize, categoryNameDisplay);
        //return  await query.Select(pt => new ProductTranslationDto{
        //        Id = pt.Id,
        //        ProductId = pt.ProductId,
        //        Name = pt.Name,
        //        ShortDescription = pt.Description.Split(new string []
        //        {
        //             "\n"
        //        },StringSplitOptions.RemoveEmptyEntries)[0],
        //        Descriptions = pt.Description.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList() ,
        //        Category = pt.Category.Replace("-"," "),
        //        AverageRate = pt.Product.AverageRate,
        //        Variants = pt.Product.Variants.Where(a=>a.LanguageCode == "en").ToList(),
        //        Price = pt.Product.Price,
        //        PriceAfterDiscount = pt.Product.PriceAfterDiscount,
        //        SKU = pt.Product.SKU,
        //    IsInWishList = wishlistIds.Contains(pt.Id),
        //    Images = pt.Product.Images.Select(img => new ProductImageDto()
        //        {
        //            Path = img.RelativePath
        //        }).ToList(),
        //        Colors = pt.Product.Colors.Where(a=>a.LanguageCode == "en").Select(c => c.Color).ToList()
        //    })
        //    .ToListAsync<ProductTranslationDto>();;
    }



    //========================
    //public async Task<PagedResult<ProductResponseDto>> GetArabicProductById(QueryParameters queryParameters, string? userId = null)
    //{
    //    var query = ctx.Products.AsQueryable();
    //    query.Include(c => c.CategoryImage);
    //    query.Include(c => c.NewProductColors);
    //    if (queryParameters.search != null)
    //    {

    //            query = query.Where(a => a.Name.Contains(queryParameters.search) || a.Descriptions.Contains(queryParameters.search));



    //    }
    //    string categoryNameDisplay = "All Categories";
    //    //if (!string.IsNullOrEmpty(queryParameters.category))
    //    //{
    //    //    var cat = queryParameters.category.ToLower();
    //    //    query = query.Where(a => a.Category.ToLower() == cat);
    //    //    categoryNameDisplay = queryParameters.category;
    //    //}
    //    if (!string.IsNullOrEmpty(queryParameters.category))
    //    {
    //        var cat = queryParameters.category.ToLower();
    //        query = query.Where(a => a.CategoryImage.CategoryValue.ToLower() == cat);
    //        var category = await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
    //        if (category != null)
    //        {
    //            categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

    //        }
    //        else
    //        {
    //            categoryNameDisplay = queryParameters.category;
    //        }
    //    }
    //    int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;

    //    //////////////
    //    var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);
    //    /////////////////////


    //    if (queryParameters.id > 0)
    //    {
    //        query = query.Where(a => a.Id == queryParameters.id);
    //    }
    //    //var totalCount = await query.CountAsync();

    //    //var pageSize = queryParameters.pageSize > 0 ? queryParameters.pageSize : 10;
    //    //var currentPage = queryParameters.page > 0 ? queryParameters.page : 1;

    //    //var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

    //    query = (queryParameters.order ?? "").ToLower() switch
    //    {
    //        "price_asc" => query.OrderBy(a => a.Price),
    //        "price_desc" => query.OrderByDescending(a => a.Price),
    //        "rating_desc" => query.OrderByDescending(a => a.AverageRate),
    //        "rating_asc" => query.OrderBy(a => a.AverageRate),
    //        "latest_asc" => query.OrderBy(a=>a.Created),
    //        "latest_desc" => query.OrderByDescending(a => a.Created),
    //        _ => query.OrderBy(a => a.Id) // Default ordering is good practice for pagination
    //    };


    //    //if (queryParameters.page > 0 && queryParameters.pageSize > 0)
    //    //{
    //    //    var skip = (queryParameters.page - 1) * queryParameters.pageSize;
    //    //    query = query.Skip(skip).Take(queryParameters.pageSize);
    //    //}


    //    //return await query.Select(p => new ProductResponseDto
    //    //{
    //    //    Id = p.Id,
    //    //    Name = p.Name,
    //    //    Price = p.Price,
    //    //    PriceAfterDiscount = p.PriceAfterDiscount,
    //    //    Description = p.Descriptions,
    //    //    ShortDescription = p.FullDescription,
    //    //    Category = p.Category.Replace("-"," "),
    //    //    CateogryValue = p.CategoryValue,
    //    //    SKU = p.SKU,
    //    //    IsInWishList = wishlistIds.Contains(p.Id),
    //    //    Images = p.Images.Select(img => new ProductImageDto
    //    //    {
    //    //        Path = img.RelativePath
    //    //    }).ToList(),
    //    //    Colors = p.Colors.Where(a=>a.LanguageCode == "ar").Select(c => c.Color).ToList(),
    //    //    Variants = p.Variants
    //    //        .Where(a=>a.LanguageCode == "ar")
    //    //        .Select(v => new ProductVariantDto
    //    //    {
    //    //        Id = v.Id,
    //    //        Type = v.Type,
    //    //        Size = v.Size,
    //    //        Price = v.Price,
    //    //        PriceBeforeDiscount = v.PriceBeforeDiscount ?? 0
    //    //    }).ToList()
    //    //}).ToListAsync();



    //    var dtoQuery = query.Select(p => new ProductResponseDto
    //    {
    //        Id = p.Id,
    //        Name = p.Name,
    //        Price = p.Price,
    //        PriceAfterDiscount = p.PriceAfterDiscount,
    //        //   Description = p.Descriptions.Replace("\r\n", "\n"),
    //        Descriptions = p.Descriptions
    //.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
    //.ToList(),

    //        AverageRate= p.AverageRate,


    //        ShortDescription = p.FullDescription,
    //        Category = p.CategoryImage.Category.Replace("-", " "),
    //        CateogryValue = p.CategoryImage.CategoryValue,
    //        SKU = p.SKU,

    //        PageNumber = currentPage, 

    //        IsInWishList = wishlistIds.Contains(p.Id),
    //        Images = p.Images.Select(img => new ProductImageDto { Path = img.RelativePath }).ToList(),
    //       // Colors = p.Colors.Where(a => a.LanguageCode == "ar").Select(c => c.Color).ToList(),
    //        Colors = p.NewProductColors.Where(c => c.ProductId == p.Id).Select(c => c.Color.ColorName).ToList(),
    //        Variants = p.Variants.Where(a => a.LanguageCode == "ar")
    //            .Select(v => new ProductVariantDto
    //            {
    //                Id = v.Id,
    //                Type = v.Type,
    //                Size = v.Size,
    //                Price = v.Price,
    //                PriceBeforeDiscount = v.PriceBeforeDiscount ?? 0
    //            }).ToList()
    //    });

    //    return await dtoQuery.ToPagedListAsync(
    //        queryParameters.page,
    //        queryParameters.pageSize,
    //        categoryNameDisplay
    //    );
    //}

    // public async Task<PagedResult<ProductTranslationDto>> GetEnglishProductById(QueryParameters queryParameters, string? userId = null)
    // {

    //     var query = ctx
    //         .ProductTranslations
    //         .Include(t => t.Product)
    //         .ThenInclude(p => p.NewProductColors)
    //         .ThenInclude(c => c.Color)
    //         .Include(t => t.Product)
    //         .ThenInclude(p => p.Variants)
    //         .Include(t => t.Product)
    //         .ThenInclude(t => t.Images)
    //         .Include(t => t.Product).ThenInclude(c => c.CategoryImage)
    //         .AsQueryable();
    //     if (queryParameters.search != null)
    //     {

    //             var translatedProductIds = await ctx.ProductTranslations
    //                 .Where(t => t.Name.Contains(queryParameters.search) || t.Description.Contains(queryParameters.search))
    //                 .Select(t => t.ProductId)
    //                 .ToListAsync();
    //             query = query.Where(a => translatedProductIds.Contains(a.Id));


    //     }
    //     string categoryNameDisplay = "";
    //     if (queryParameters.LanguageCode == "en")
    //         categoryNameDisplay = "All Categories";
    //     else
    //         categoryNameDisplay = "كل الاقسام";
    //     //if (!string.IsNullOrEmpty(queryParameters.category))
    //     //{
    //     //    var cat = queryParameters.category.ToLower();
    //     //    query = query.Where(a => a.Category.ToLower() == cat);
    //     //    categoryNameDisplay = queryParameters.category;
    //     //}
    //     if (!string.IsNullOrEmpty(queryParameters.category))
    //     {
    //         var cat = queryParameters.category.ToLower();
    //         query = query.Where(a => a.Product.CategoryImage.Category.ToLower() == cat);
    //         var category = await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
    //         if (category != null)
    //         {
    //             categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

    //         }
    //         else
    //         {
    //             categoryNameDisplay = queryParameters.category;
    //         }
    //     }
    //     int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
    //     var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

    //     if (queryParameters.id > 0)
    //     {
    //         query = query.Where(a => a.Id ==queryParameters.id);
    //     }

    //     //if (!string.IsNullOrEmpty(queryParameters.category))
    //     //{
    //     //    query = query.Where(a => a.Category.ToLower() == queryParameters.category.ToLower());
    //     //}

    //     //if (queryParameters.page > 0 && queryParameters.pageSize > 0)
    //     //{
    //     //    var skip = (queryParameters.page - 1) * queryParameters.pageSize;
    //     //    query = query.Skip(skip).Take(queryParameters.pageSize);
    //     //}


    //     query = queryParameters.order.ToLower() switch
    //     {
    //         "price_asc" => query.OrderBy(a => a.Price),
    //         "price_desc" => query.OrderByDescending(a => a.Price),
    //         "rating_desc" => query.OrderByDescending(a => a.AverageRate),
    //         "rating_asc" => query.OrderBy(a => a.AverageRate),
    //"latest_asc" => query.OrderBy(a => a.Product.Created),
    //"latest_desc" => query.OrderByDescending(a => a.Product.Created),
    //_ => query
    //     };
    //     var dtoQuery = query.Select(pt => new ProductTranslationDto
    //     {
    //         Id = pt.Id,
    //        // ProductId = pt.ProductId,
    //         Name = pt.Name,
    //         ShortDescription = pt.Description.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)[0],
    //         Descriptions = pt.Description.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList(),
    //         Category = pt.Product.CategoryImage.CategoryValue.Replace("-", " "),
    //         CateogryValue = pt.Product.CategoryImage.CategoryValue,
    //         AverageRate = pt.Product.AverageRate,

    //         PageNumber = currentPage,

    //         //Variants = pt.Product.Variants.Where(a => a.LanguageCode == "ar").ToList(),
    //         Variants = pt.Product.Variants.Where(a => a.LanguageCode == "ar")
    //             .Select(v => new ProductVariantDto
    //             {
    //                 Id = v.Id,
    //                 Type = v.EnglishType,
    //                 Size = v.EnglishSize,
    //                 Price = v.Price,
    //                 PriceBeforeDiscount = v.PriceBeforeDiscount ?? 0
    //             }).ToList(),

    //     Price = pt.Product.Price,
    //         PriceAfterDiscount = pt.Product.PriceAfterDiscount,
    //         SKU = pt.Product.SKU,
    //         IsInWishList = wishlistIds.Contains(pt.Id), 
    //         Images = pt.Product.Images.Select(img => new ProductImageDto()
    //         {
    //             Path = img.RelativePath
    //         }).ToList(),
    //         //  Colors = pt.Product.Colors.Where(a => a.LanguageCode == "en").Select(c => c.Color).ToList()
    //         //  Colors = pt.Product.NewProductColors.Where(p=>p.ProductId==pt.Id).Select(c => c.Color.EnglishColor).ToList()
    //         Colors = pt.Product.NewProductColors
    // .Where(c => c.ProductId == pt.Id)
    // .Select(c => new ColorDto
    // {
    //     ColorId = c.Color.Id,
    //     ColorName = c.Color.EnglishColor
    // })
    // .ToList(),

    //     });

    //     return await dtoQuery.ToPagedListAsync(queryParameters.page, queryParameters.pageSize, categoryNameDisplay);
    //     //return  await query.Select(pt => new ProductTranslationDto{
    //     //        Id = pt.Id,
    //     //        ProductId = pt.ProductId,
    //     //        Name = pt.Name,
    //     //        ShortDescription = pt.Description.Split(new string []
    //     //        {
    //     //             "\n"
    //     //        },StringSplitOptions.RemoveEmptyEntries)[0],
    //     //        Descriptions = pt.Description.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList() ,
    //     //        Category = pt.Category.Replace("-"," "),
    //     //        AverageRate = pt.Product.AverageRate,
    //     //        Variants = pt.Product.Variants.Where(a=>a.LanguageCode == "en").ToList(),
    //     //        Price = pt.Product.Price,
    //     //        PriceAfterDiscount = pt.Product.PriceAfterDiscount,
    //     //        SKU = pt.Product.SKU,
    //     //    IsInWishList = wishlistIds.Contains(pt.Id),
    //     //    Images = pt.Product.Images.Select(img => new ProductImageDto()
    //     //        {
    //     //            Path = img.RelativePath
    //     //        }).ToList(),
    //     //        Colors = pt.Product.Colors.Where(a=>a.LanguageCode == "en").Select(c => c.Color).ToList()
    //     //    })
    //     //    .ToListAsync<ProductTranslationDto>();;
    // }





    public async Task<List<ProductENARDto>> GetArabicAndEnglishProductById(QueryParameters queryParameters, string? userId = null)
    {

        //var query = ctx
        //    .ProductTranslations
        //    .Include(t => t.Product)
        //    .ThenInclude(p=>p.NewProductColors)
        //    .Include(t => t.Product)
        //    .ThenInclude(p => p.Variants)
        //    .Include(t => t.Product)
        //    .ThenInclude(t => t.Images)
        //    .Include(t => t.Product).ThenInclude(c => c.CategoryImage)
        //    .AsQueryable();
        var query = ctx.Products.AsNoTracking().AsQueryable();
        query = query.Include(c => c.CategoryImage);
        query = query.Include(c => c.NewProductColors).ThenInclude(c => c.Color);
        if (!string.IsNullOrEmpty(queryParameters.search))
        {
            
                var translatedProductIds = await ctx.Products
                    .Where(t => t.Name.Contains(queryParameters.search) || t.DescriptionsEN.Contains(queryParameters.search))
                    .Select(t => t.Id)
                    .ToListAsync();
                query = query.Where(a => translatedProductIds.Contains(a.Id));
            

        }
        string categoryNameDisplay = "";
        if (queryParameters.LanguageCode == "en")
            categoryNameDisplay = "All Categories";
        else
            categoryNameDisplay = "كل الاقسام";
        //if (!string.IsNullOrEmpty(queryParameters.category))
        //{
        //    var cat = queryParameters.category.ToLower();
        //    query = query.Where(a => a.Category.ToLower() == cat);
        //    categoryNameDisplay = queryParameters.category;
        //}
        if (!string.IsNullOrEmpty(queryParameters.category))
        {
            var cat = queryParameters.category.ToLower();
            query = query.Where(a => a.CategoryImage.Category.ToLower() == cat);
            var category = await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
            if (category != null)
            {
                categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

            }
            else
            {
                categoryNameDisplay = queryParameters.category;
            }
        }
        //int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        if (queryParameters.id > 0)
        {
            query = query.Where(a => a.Id ==queryParameters.id);
        }

        //if (!string.IsNullOrEmpty(queryParameters.category))
        //{
        //    query = query.Where(a => a.Category.ToLower() == queryParameters.category.ToLower());
        //}
        
        //if (queryParameters.page > 0 && queryParameters.pageSize > 0)
        //{
        //    var skip = (queryParameters.page - 1) * queryParameters.pageSize;
        //    query = query.Skip(skip).Take(queryParameters.pageSize);
        //}
         
        
        query = queryParameters.order.ToLower() switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "rating_desc" => query.OrderByDescending(a => a.AverageRate),
            "rating_asc" => query.OrderBy(a => a.AverageRate),
			"latest_asc" => query.OrderBy(a => a.Created),
			"latest_desc" => query.OrderByDescending(a => a.Created),
			_ => query
        };
        var dtoQuery = query.Select(pt => new ProductENARDto
        {
            Id = pt.Id,
           // ProductId = pt.ProductId,
            nameEn = pt.NameEN,
            nameAr=pt.Name,
            fullDescriptionAr=pt.Descriptions,
            shortDescriptionAr=pt.FullDescription,
            shortDescriptionEn = pt.ShortDescriptionEn,//.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)[0],
            fullDescriptionEn = pt.DescriptionsEN,//.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList(),
           // Category = pt.CategoryImage.CategoryValue.Replace("-", " "),
           // ArabicCategory=pt.CategoryImage.Category.Replace("-", " "),
            categoryValue = pt.CategoryImage.CategoryValue,
           // AverageRate = pt.AverageRate,
            availableSizes = pt.Variants
    .Where(v => v.LanguageCode == "ar")
    .Select(v => new { v.EnglishSize, v.Size })   // DISTINCT هنا
    .Distinct()
    .Select(v => new availableSize
    {
        Id = Guid.NewGuid().ToString(),
        sizeEn = v.EnglishSize,
        sizeAr = v.Size
    })
    .ToList(),

            availableTypes = pt.Variants
    .Where(v => v.LanguageCode == "ar")
    .Select(v => new { v.EnglishType, v.Type })
    .Distinct()
    .Select(v => new availableType
    {
        Id = Guid.NewGuid().ToString(),
        typeEn = v.EnglishType,
        typeAr = v.Type
    })
    .ToList(),

            //PageNumber = currentPage,

            //Variants = pt.Product.Variants.Where(a => a.LanguageCode == "ar").ToList(),
            Variants = pt.Variants.Where(a => a.LanguageCode == "ar")
                .Select(v => new ProductVariantENARDto
                {
                    Id = v.Id,
                    ArabicType=v.Type,
                    ArabicSize=v.Size,
                    Type = v.EnglishType,
                    Size = v.EnglishSize,
                    Price = v.Price,
                    PriceBeforeDiscount = v.PriceBeforeDiscount ?? 0
                }).ToList(),
       
        Price = pt.Price,
            PriceAfterDiscount = pt.PriceAfterDiscount,
            sku = pt.SKU,
           // IsInWishList = wishlistIds.Contains(pt.Id), 
            Images = pt.Images.Select(img => new ProductImageWithIdDto()
            {
                Id= img.Id,
                Path = img.RelativePath
            }).ToList(),
          //  Colors = pt.Product.Colors.Where(a => a.LanguageCode == "en").Select(c => c.Color).ToList()
            colors = pt.NewProductColors.Where(p=>p.ProductId==pt.Id).Select(c => new ColorDto0
            {
               // Id = c.Color.Id,
                NameEn = c.Color.EnglishColor,
                NameAr = c.Color.ColorName
            }).ToList(),
           // ArabicColors=pt.NewProductColors.Where(p=>p.ProductId==pt.Id).Select(c => c.Color.ColorName).ToList()
        }).ToList();
        return  dtoQuery;
      //  return await dtoQuery.ToARENListAsync( categoryNameDisplay);
        //return  await query.Select(pt => new ProductTranslationDto{
        //        Id = pt.Id,
        //        ProductId = pt.ProductId,
        //        Name = pt.Name,
        //        ShortDescription = pt.Description.Split(new string []
        //        {
        //             "\n"
        //        },StringSplitOptions.RemoveEmptyEntries)[0],
        //        Descriptions = pt.Description.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList() ,
        //        Category = pt.Category.Replace("-"," "),
        //        AverageRate = pt.Product.AverageRate,
        //        Variants = pt.Product.Variants.Where(a=>a.LanguageCode == "en").ToList(),
        //        Price = pt.Product.Price,
        //        PriceAfterDiscount = pt.Product.PriceAfterDiscount,
        //        SKU = pt.Product.SKU,
        //    IsInWishList = wishlistIds.Contains(pt.Id),
        //    Images = pt.Product.Images.Select(img => new ProductImageDto()
        //        {
        //            Path = img.RelativePath
        //        }).ToList(),
        //        Colors = pt.Product.Colors.Where(a=>a.LanguageCode == "en").Select(c => c.Color).ToList()
        //    })
        //    .ToListAsync<ProductTranslationDto>();;
    }

    //public async Task<PagedResult<ProductOverviewDto>> GetProductsOverview(QueryParameters queryParameters, string? userId = null)
    //{
    //  //  queryParameters.page = queryParameters.page > 0 ? queryParameters.page   :  1;
    //    queryParameters.pageSize = queryParameters.pageSize > 0 ? queryParameters.pageSize : 12;

    //    var query = ctx
    //        .Products
    //        .Include(p => p.Variants)   
    //        .Include(t=>t.Images)
    //        .AsQueryable();

    //    string categoryNameDisplay = "All Categories";
    //    if (!string.IsNullOrEmpty(queryParameters.category))
    //    {
    //        var cat = queryParameters.category.ToLower();
    //        query = query.Where(a => a.Category.ToLower() == cat);
    //        categoryNameDisplay = queryParameters.category;
    //    }
    //    int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
    //    var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

    //    if (!string.IsNullOrEmpty(queryParameters.category))
    //    {
    //        query = query.Where(a => a.Category.ToLower() == queryParameters.category.ToLower());
    //    }

    //    //if (queryParameters.page > 0 && queryParameters.pageSize > 0)
    //    //{
    //    //    var skip = (queryParameters.page - 1) * queryParameters.pageSize;
    //    //    query = query.Skip(skip).Take(queryParameters.pageSize);
    //    //}


    //    query = queryParameters.order.ToLower() switch
    //    {
    //        "price_asc" => query.OrderBy(a => a.Price),
    //        "price_desc" => query.OrderByDescending(a => a.Price),
    //        "rating_desc" => query.OrderByDescending(a => a.AverageRate),
    //        "rating_asc" => query.OrderBy(a => a.AverageRate),
    //        "latest_asc" => query.OrderBy(a=>a.Created),
    //        "latest_desc" => query.OrderByDescending(a => a.Created),
    //        _ => query
    //    };

    //    var dtoQuery =  query.Select(p => new ProductOverviewDto
    //    {
    //        Id = p.Id,
    //        Name = p.Name,
    //        Price = p.Price,
    //        PriceAfterDiscount = p.PriceAfterDiscount,
    //        PageNumber = currentPage,
    //        ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().RelativePath : null,
    //        TotalRatingPeople = p.TotalRatePeople,
    //        AverageRatingPeople = p.AverageRate,
    //        IsInWishList = wishlistIds.Contains(p.Id),
    //    });

    //    return await dtoQuery.ToPagedListAsync(queryParameters.page, queryParameters.pageSize, categoryNameDisplay);
    //}
    public async Task<PagedResult<ProductOverviewDto>> GetProductsOverview(QueryParameters queryParameters, string? userId = null)
    {
       
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        int pageSize = queryParameters.pageSize;// > 0 ? queryParameters.pageSize : 12;

        var query = ctx.Products.AsNoTracking().AsQueryable();
          query.Include(c => c.CategoryImage);
        if(queryParameters.search != null)
        {
            //if (queryParameters.LanguageCode == "ar")
            //{
            //    query = query.Where(a => a.Name.Contains(queryParameters.search) || a.Descriptions.Contains(queryParameters.search));
            //}
            //else
            //{
                var translatedProductIds = await ctx.Products
                    .Where(t => t.NameEN.Contains(queryParameters.search) || t.DescriptionsEN.Contains(queryParameters.search)
                    || t.Name.Contains(queryParameters.search) || t.Descriptions.Contains(queryParameters.search))
                    .Select(t => t.Id)
                    .ToListAsync();
                query = query.Where(a => translatedProductIds.Contains(a.Id));
            //}

        }
        string categoryNameDisplay = "";
        if (queryParameters.LanguageCode == "en")
            categoryNameDisplay = "All Categories";
        else
            categoryNameDisplay = "كل الاقسام";
        if (!string.IsNullOrEmpty(queryParameters.category))
        {
            var cat = queryParameters.category.ToLower();
            query = query.Where(a => a.CategoryImage.CategoryValue.ToLower() == cat);
            var category= await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
            if (category != null)
            {
                categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

            }
            else
            {
                categoryNameDisplay = queryParameters.category;
            }
        }

        query = (queryParameters.order ?? "").ToLower() switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "rating_desc" => query.OrderByDescending(a => a.AverageRate),
            "rating_asc" => query.OrderBy(a => a.AverageRate),
            "latest_asc" => query.OrderBy(a => a.Created),
            "latest_desc" => query.OrderByDescending(a => a.Created),
            _ => query.OrderBy(a => a.Id) 
        };

        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        var dtoQuery = query.Select(p => new ProductOverviewDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            PriceAfterDiscount = p.PriceAfterDiscount,
            CategoryAr = p.CategoryImage.Category.Replace("-", " "),
            CategoryValue= p.CategoryImage.CategoryValue,
            PageNumber = currentPage, 

            ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().RelativePath : null,

            TotalPeopleRating = p.TotalRatePeople,
            AverageRatingPeople = p.AverageRate,
            IsInWishList = wishlistIds.Contains(p.Id),
        });

       
        return await dtoQuery.ToPagedListAsync(currentPage, pageSize, categoryNameDisplay);
    }
  
    public async Task<PagedResult<ProductOverviewForDashboardDto>> GetProductsOverviewForDashboard(QueryParameters queryParameters, string? userId = null)
    {
       
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        int pageSize = queryParameters.pageSize;// > 0 ? queryParameters.pageSize : 12;

        var query = ctx.Products.AsNoTracking().AsQueryable();
        query.Include(c => c.CategoryImage);
        if(queryParameters.search != null)
        {
            //if (queryParameters.LanguageCode == "ar")
            //{
            //    query = query.Where(a => a.Name.Contains(queryParameters.search) || a.Descriptions.Contains(queryParameters.search));
            //}
            //else
            //{
                var translatedProductIds = await ctx.Products
                    .Where(t => t.NameEN.Contains(queryParameters.search) || t.DescriptionsEN.Contains(queryParameters.search)
                    || t.Name.Contains(queryParameters.search) || t.Descriptions.Contains(queryParameters.search))
                    .Select(t => t.Id)
                    .ToListAsync();
                query = query.Where(a => translatedProductIds.Contains(a.Id));
            //}

        }
        string categoryNameDisplay = "";
        if (queryParameters.LanguageCode == "en")
            categoryNameDisplay = "All Categories";
        else
            categoryNameDisplay = "كل الاقسام";
        if (!string.IsNullOrEmpty(queryParameters.category))
        {
            var cat = queryParameters.category.ToLower();
            query = query.Where(a => a.CategoryImage.CategoryValue.ToLower() == cat);
            var category= await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
            if (category != null)
            {
                categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

            }
            else
            {
                categoryNameDisplay = queryParameters.category;
            }
        }

        query = (queryParameters.order ?? "").ToLower() switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "rating_desc" => query.OrderByDescending(a => a.AverageRate),
            "rating_asc" => query.OrderBy(a => a.AverageRate),
            "latest_asc" => query.OrderBy(a => a.Created),
            "latest_desc" => query.OrderByDescending(a => a.Created),
            _ => query.OrderBy(a => a.Id) 
        };

        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        var dtoQuery = query.Select(p => new ProductOverviewForDashboardDto
        {
            Id = p.Id,
            sku=p.SKU,
            Name = p.Name,
            Price = p.Price,
            PriceAfterDiscount = p.PriceAfterDiscount,
            Category = p.CategoryImage.Category.Replace("-", " "),
            PageNumber = currentPage, 

            ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().RelativePath : null,

     
        });

       
        return await dtoQuery.ToPagedListAsync(currentPage, pageSize, categoryNameDisplay);
    }
  
    
    public async Task<PagedResult<ProductTranslationOverviewDto>> GetProductsTranslationOverview(QueryParameters queryParameters, string? userId = null)
    {
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        int pageSize = queryParameters.pageSize;// > 0 ? queryParameters.pageSize : 12;

        //var query = ctx.ProductTranslations.AsQueryable();
        //query.Include(t => t.Product).ThenInclude(c => c.CategoryImage);
        var query = ctx.Products.AsNoTracking().AsQueryable();
        query.Include(c => c.CategoryImage);
        if (queryParameters.search != null)
        {
            //if (queryParameters.LanguageCode == "ar")
            //{
            //    query = query.Where(a => a.Name.Contains(queryParameters.search) || a.DescriptionsEN.Contains(queryParameters.search));
            //}
            //else
            //{
            //    var translatedProductIds = await ctx.ProductTranslations
            //        .Where(t => t.Name.Contains(queryParameters.search) || t.Description.Contains(queryParameters.search))
            //        .Select(t => t.ProductId)
            //        .ToListAsync();
            //    query = query.Where(a => translatedProductIds.Contains(a.Id));
            //}
            //if (queryParameters.LanguageCode == "ar")
            //{
            //    query = query.Where(a => a.Name.Contains(queryParameters.search) || a.Descriptions.Contains(queryParameters.search));
            //}
            //else
            //{
                var translatedProductIds = await ctx.Products
                    .Where(t => t.NameEN.Contains(queryParameters.search) || t.DescriptionsEN.Contains(queryParameters.search)
                    || t.Name.Contains(queryParameters.search) || t.Descriptions.Contains(queryParameters.search))
                    .Select(t => t.Id)
                    .ToListAsync();
                query = query.Where(a => translatedProductIds.Contains(a.Id));
            //}
        }
        string categoryNameDisplay = "";
        if (queryParameters.LanguageCode == "en")
            categoryNameDisplay = "All Categories";
        else
            categoryNameDisplay = "كل الاقسام";
        //if (!string.IsNullOrEmpty(queryParameters.category))
        //{
        //    var cat = queryParameters.category.ToLower();
        //    query = query.Where(a => a.Category.ToLower() == cat);
        //    categoryNameDisplay = queryParameters.category;
        //}
        if (!string.IsNullOrEmpty(queryParameters.category))
        {
            var cat = queryParameters.category.ToLower();
            query = query.Where(a => a.CategoryImage.CategoryValue.ToLower() == cat);
           // query = query.Where(a => a.Category.ToLower() == cat);
            var category = await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
            if (category != null)
            {
                categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

            }
            else
            {
                categoryNameDisplay = queryParameters.category;
            }
        }
        query = (queryParameters.order ?? "").ToLower() switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "rating_desc" => query.OrderByDescending(a => a.AverageRate),
            "rating_asc" => query.OrderBy(a => a.AverageRate),
            "latest_asc" => query.OrderBy(a => a.Created),  
            "latest_desc" => query.OrderByDescending(a => a.Created),
            _ => query.OrderBy(a => a.Id) 
        };

        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        var dtoQuery = query.Select(p => new ProductTranslationOverviewDto
        {
            Id = p.Id,
            Name = p.NameEN,
            Price = p.Price,
            PriceAfterDiscount = p.PriceAfterDiscount,
            CategoryEn = p.CategoryImage.CategoryValue.Replace("-", " "),
            CategoryValue = p.CategoryImage.CategoryValue,
            PageNumber = currentPage,

            ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().RelativePath : null,

            TotalPeopleRating = p.TotalRatePeople,
            AverageRatingPeople = p.AverageRate,
            IsInWishList = wishlistIds.Contains(p.Id)
        });

        return await dtoQuery.ToPagedListAsync(currentPage, pageSize, categoryNameDisplay);
    }
    public async Task<PagedResult<ProductTranslationOverviewForDashboardDto>> GetProductsTranslationOverviewForDashboard(QueryParameters queryParameters, string? userId = null)
    {
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        int pageSize = queryParameters.pageSize;// > 0 ? queryParameters.pageSize : 12;

        //var query = ctx.ProductTranslations.AsQueryable();
        //query.Include(t => t.Product).ThenInclude(c => c.CategoryImage);
        var query = ctx.Products.AsNoTracking().AsQueryable();
        query.Include(c => c.CategoryImage);
        if (queryParameters.search != null)
        {
            //if (queryParameters.LanguageCode == "ar")
            //{
            //    query = query.Where(a => a.Name.Contains(queryParameters.search) || a.DescriptionsEN.Contains(queryParameters.search));
            //}
            //else
            //{
            //    var translatedProductIds = await ctx.Products
            //        .Where(t => t.NameEN.Contains(queryParameters.search) || t.DescriptionsEN.Contains(queryParameters.search))
            //        .Select(t => t.Id)
            //        .ToListAsync();
            //    query = query.Where(a => translatedProductIds.Contains(a.Id));
            //}
            //if (queryParameters.LanguageCode == "ar")
            //{
            //    query = query.Where(a => a.Name.Contains(queryParameters.search) || a.Descriptions.Contains(queryParameters.search));
            //}
            //else
            //{
            var translatedProductIds = await ctx.Products
                 .Where(t => t.NameEN.Contains(queryParameters.search) || t.DescriptionsEN.Contains(queryParameters.search)
                 || t.Name.Contains(queryParameters.search) || t.Descriptions.Contains(queryParameters.search))
                 .Select(t => t.Id)
                 .ToListAsync();
            query = query.Where(a => translatedProductIds.Contains(a.Id));
            //}
        }
        string categoryNameDisplay = "";
        if (queryParameters.LanguageCode == "en")
            categoryNameDisplay = "All Categories";
        else
            categoryNameDisplay = "كل الاقسام";
        //if (!string.IsNullOrEmpty(queryParameters.category))
        //{
        //    var cat = queryParameters.category.ToLower();
        //    query = query.Where(a => a.Category.ToLower() == cat);
        //    categoryNameDisplay = queryParameters.category;
        //}
        if (!string.IsNullOrEmpty(queryParameters.category))
        {
            var cat = queryParameters.category.ToLower();
            query = query.Where(a => a.CategoryImage.CategoryValue.ToLower() == cat);
           // query = query.Where(a => a.Category.ToLower() == cat);
            var category = await ctx.CategoryImages.FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == cat);
            if (category != null)
            {
                categoryNameDisplay = queryParameters.LanguageCode == "en" ? category.CategoryValue.Replace("-", " ") : category.Category.Replace("-", " ");

            }
            else
            {
                categoryNameDisplay = queryParameters.category;
            }
        }
        query = (queryParameters.order ?? "").ToLower() switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "rating_desc" => query.OrderByDescending(a => a.AverageRate),
            "rating_asc" => query.OrderBy(a => a.AverageRate),
            "latest_asc" => query.OrderBy(a => a.Created),  
            "latest_desc" => query.OrderByDescending(a => a.Created),
            _ => query.OrderBy(a => a.Id) 
        };

        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        var dtoQuery = query.Select(p => new ProductTranslationOverviewForDashboardDto
        {
            Id = p.Id,
            sku=p.SKU,
            Name = p.NameEN,
            Price = p.Price,
            PriceAfterDiscount = p.PriceAfterDiscount,
            Category = p.CategoryImage.CategoryValue.Replace("-", " "),
            PageNumber = currentPage,

            ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().RelativePath : null,

        });

        return await dtoQuery.ToPagedListAsync(currentPage, pageSize, categoryNameDisplay);
    }
    //public async Task<PagedResult<ProductTranslationOverviewDto>> GetProductsTranslationOverview(QueryParameters queryParameters, string? userId = null)
    //{
    //   // queryParameters.page = queryParameters.page > 0 ? queryParameters.page   :  1;
    //    queryParameters.pageSize = queryParameters.pageSize > 0 ? queryParameters.pageSize : 12;


    //    var query = ctx
    //        .ProductTranslations
    //        .Include(t => t.Product)            
    //        .ThenInclude(p => p.Variants)   
    //        .Include(p=>p.Product)
    //        .ThenInclude(t=>t.Images)
    //        .AsQueryable();
    //    string categoryNameDisplay = "All Categories";
    //    if (!string.IsNullOrEmpty(queryParameters.category))
    //    {
    //        var cat = queryParameters.category.ToLower();
    //        query = query.Where(a => a.Category.ToLower() == cat);
    //        categoryNameDisplay = queryParameters.category;
    //    }
    //    int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
    //    var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

    //    //if (!string.IsNullOrEmpty(queryParameters.category))
    //    //{
    //    //    query = query.Where(a => a.Category.ToLower() == queryParameters.category.ToLower());

    //    //}

    //    //if (queryParameters.page > 0 && queryParameters.pageSize > 0)
    //    //{
    //    //    var skip = (queryParameters.page - 1) * queryParameters.pageSize;
    //    //    query = query.Skip(skip).Take(queryParameters.pageSize);
    //    //}


    //    query = queryParameters.order.ToLower() switch
    //    {
    //        "price_asc" => query.OrderBy(a => a.Price),
    //        "price_desc" => query.OrderByDescending(a => a.Price),
    //        "rating_desc" => query.OrderByDescending(a => a.AverageRate),
    //        "rating_asc" => query.OrderBy(a => a.AverageRate),
    //        _ => query
    //    };


    //    var dtoQuery =  query.Select(p => 
    //        new ProductTranslationOverviewDto
    //    {
    //        Id = p.Id,
    //        Name = p.Name,
    //        Price = p.Price,
    //        PriceAfterDiscount = p.PriceAfterDiscount,
    //        PageNumber = currentPage,
    //            ImageUrl = p.Product.Images.FirstOrDefault().RelativePath,
    //        TotalPeopleRating = p.Product.TotalRatePeople,
    //        AverageRatingPeople = p.Product.AverageRate,
    //         IsInWishList = wishlistIds.Contains(p.Id)
    //        });

    //    return await dtoQuery.ToPagedListAsync(queryParameters.page, queryParameters.pageSize, categoryNameDisplay);
    //}

    public async Task<List<ProductOverviewDto>> GetTop4RatedProducts(QueryParameters queryParameters, string? userId = null)
    {
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        return await ctx?.Products
            .OrderByDescending(a => a.AverageRate)
            .Include(a=>a.Variants)
            .Include(a=>a.CategoryImage)
            .Include(a => a.Images)
            .Take(4)
            .Select(p => new ProductOverviewDto
            {
                Id = p.Id,
                Name = p.Name,
                CategoryAr = p.CategoryImage.Category.Replace("-", " "),
                CategoryValue= p.CategoryImage.CategoryValue,
                Price = p.Price,
                PriceAfterDiscount = p.PriceAfterDiscount,
                ImageUrl = p.Images.FirstOrDefault().RelativePath,
                TotalPeopleRating = p.TotalRatePeople,
                AverageRatingPeople = p.AverageRate,
                IsInWishList = wishlistIds.Contains(p.Id)
            }).ToListAsync();
             
    }

    public async Task<List<ProductOverviewDto>> GetTop4RecentProducts(QueryParameters query, string? userId = null)
    {
        var wishlistIds = await GetWishlistIdsAsync(userId, query.Ids);

        return await ctx?.Products
            .OrderByDescending(a => a.Created)
            .Include(a => a.Variants)
            .Include(a => a.Images)
            .OrderByDescending(a=>a.Created)
            .Take(4)
            .Select(p => new ProductOverviewDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                PriceAfterDiscount = p.PriceAfterDiscount,
                ImageUrl = p.Images.FirstOrDefault().RelativePath,
                TotalPeopleRating = p.TotalRatePeople,
                AverageRatingPeople = p.AverageRate,
                  IsInWishList = wishlistIds.Contains(p.Id)
            }).ToListAsync();
    }

    public async Task<List<ProductTranslationOverviewDto>> GetTop4RecentProductTranslations(QueryParameters queryParameters, string? userId = null)
    {
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);
                    
        return await ctx?
            //.ProductTranslations
            //.Include(t => t.Product)
            //.ThenInclude(p => p.Variants)  
            //.Include(t => t.Product)
            //.ThenInclude(p => p.Images)    
            .Products
            .OrderByDescending(a => a.Created)
            .Include(a => a.Variants)
            .Include(a => a.Images)
            .Take(4)
            .Select(pt => new ProductTranslationOverviewDto{
                Id = pt.Id,
                Name = pt.NameEN,
                Price = pt.Price,
                PriceAfterDiscount = pt.PriceAfterDiscount,
                ImageUrl = pt.Images.FirstOrDefault().RelativePath,
                TotalPeopleRating = pt.TotalRatePeople,
                AverageRatingPeople = pt.AverageRate,
                 IsInWishList = wishlistIds.Contains(pt.Id)
            })
            .ToListAsync<ProductTranslationOverviewDto>();
    }

    public async Task<List<ProductTranslationOverviewDto>> GetTop4RatedProductTranslations(QueryParameters queryParameters, string? userId = null)
    {
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);

        return await //ctx?.ProductTranslations
        //    .Include(t => t.Product).ThenInclude(p => p.Variants)
        //    .Include(t => t.Product).ThenInclude(p => p.Images)
        ctx?.Products
            .OrderByDescending(a => a.AverageRate)
            .Include(a => a.Variants)
            .Include(a => a.CategoryImage)
            .Include(a => a.Images)
            .Take(4)
            .Select(pt => new ProductTranslationOverviewDto(){
                Id = pt.Id,
                Name = pt.NameEN,
                Price = pt.Price,
                PriceAfterDiscount = pt.PriceAfterDiscount,
                ImageUrl = pt.Images.FirstOrDefault().RelativePath,
                TotalPeopleRating = pt.TotalRatePeople,
                AverageRatingPeople = pt.AverageRate,
                 IsInWishList = wishlistIds.Contains(pt.Id)
            })
            .ToListAsync<ProductTranslationOverviewDto>();
    }
    
    public List<string> GetProductTranslationCategories()
    {
        return ctx.Products
            .Select(t => t.CategoryImage.Category)
            .Distinct()
            .ToList();
    }

    public async Task<PagedResult<ProductOverviewDto>> GetProductsByCategory(QueryParameters queryParameters , int Id, string? userId = null)
    {
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        int pageSize = queryParameters.pageSize > 0 ? queryParameters.pageSize : 12;
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);
       // var product=ctx.Products.Include(c=>c.CategoryImage).FirstOrDefault(a => a.Id == Id);
        var product=ctx.Products.FirstOrDefault(a => a.Id == Id);
        var categ = ctx.CategoryImages.FirstOrDefault(c => c.Id == product.CategoryFK);
        if (product == null) 
            {
            return new PagedResult<ProductOverviewDto>();
        }
        var dtoQuery =ctx.Products.Include(a => a.CategoryImage)
            .Where(a => a.CategoryImage.CategoryValue.ToLower() == categ.CategoryValue.ToLower() && a.Id != Id)
            .Include(a => a.Images)
            .Take(4)
            .Select(p => new ProductOverviewDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                PriceAfterDiscount = p.PriceAfterDiscount,
                ImageUrl = p.Images.FirstOrDefault().RelativePath,
                TotalPeopleRating = p.TotalRatePeople,
                AverageRatingPeople = p.AverageRate,
                IsInWishList = wishlistIds.Contains(p.Id)
            }).AsQueryable();
        string categoryNameDisplay =  product.CategoryImage.Category.Replace("-", " ");
        return await dtoQuery.ToPagedListAsync(currentPage, pageSize, categoryNameDisplay);

    }


    public async Task<PagedResult<ProductTranslationOverviewDto>> GetProductTranslationsByCategory(QueryParameters queryParameters, int Id, string? userId = null)
    {
        int currentPage = queryParameters.page > 0 ? queryParameters.page : 1;
        int pageSize = queryParameters.pageSize > 0 ? queryParameters.pageSize : 12;
        var wishlistIds = await GetWishlistIdsAsync(userId, queryParameters.Ids);
        //  var product = ctx.Products.Include(c => c.CategoryImage).FirstOrDefault(a => a.Id == Id);
        var product = ctx.Products.FirstOrDefault(a => a.Id == Id);
        var categ = ctx.CategoryImages.FirstOrDefault(c => c.Id == product.CategoryFK);

        if (product == null)
        {
            return new PagedResult<ProductTranslationOverviewDto>();
        }
        var dtoQuery = ctx.Products.Include(a => a.CategoryImage)//ctx.ProductTranslations.Include(a => a.Product).ThenInclude(c => c.CategoryImage)
            .Where(a => a.CategoryImage.CategoryValue.ToLower() == categ.CategoryValue.ToLower() && a.Id != Id)
           // .Include(a=>a.Product)
            .Include(a => a.Images)
            .Take(4)
            .Select(pt => new ProductTranslationOverviewDto(){
                Id = pt.Id,
                Name = pt.NameEN,
                Price = pt.Price,
                ImageUrl = pt.Images.FirstOrDefault().RelativePath,
                CategoryValue = pt.CategoryImage.CategoryValue,
                CategoryEn=pt.CategoryImage.CategoryValue.Replace("-", " "),
                PriceAfterDiscount = pt.PriceAfterDiscount,
                TotalPeopleRating = pt.TotalRatePeople,
                AverageRatingPeople = pt.AverageRate,
                IsInWishList = wishlistIds.Contains(pt.Id)
            }).AsQueryable();


        string categoryNameDisplay = product.CategoryImage.CategoryValue.Replace("-", " ");
        return await dtoQuery.ToPagedListAsync(currentPage, pageSize, categoryNameDisplay);
    }

    public void SaveChanges()
    {
        ctx.SaveChanges();
    }
     public bool UpdateProduct(Product product)
    {
        try
        {
            ctx.Products.Update(product);
            return ctx.SaveChanges() > 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }


    public async Task<bool> AddProduct(ProductAddDto0 productDto, List<IFormFile>? images,
 IWebHostEnvironment env, IConfiguration config)
    {
        using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var category = await ctx.CategoryImages
                .FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == productDto.CategoryValue.ToLower());

            if (category == null)
                return false;

            decimal basePrice = productDto.Price;
            decimal basePriceAfterDiscount = productDto.PriceAfterDiscount;

            if (basePrice == 0 && productDto.Variants != null && productDto.Variants.Any())
            {
                var firstVariant = productDto.Variants.First();
                basePrice = firstVariant.Price; // السعر بعد الخصم كأساس
                basePriceAfterDiscount = /*firstVariant.Price - */firstVariant.PriceAfterDiscount; // الفرق
            }

            // 1. إنشاء المنتج الأساسي
            var product = new Product
            {
                Name = productDto.NameAr,
                NameEN = productDto.NameEn,
                Descriptions = productDto.FullDescriptionAr,
                DescriptionsEN = productDto.FullDescriptionEn,
                ShortDescriptionEn = productDto.ShortDescriptionEn,
                FullDescription = productDto.ShortDescriptionAr,
                //   FullDescription = productDto.FullDescriptionAr,
                Price = basePrice,
                PriceAfterDiscount = basePriceAfterDiscount,
                SKU = productDto.SKU,
                CategoryFK = category.Id,
                Created = DateTime.UtcNow,
                AverageRate = 0,
                TotalRatePeople = 0,
                LastUpdate = DateTime.UtcNow,
                RatingSum = 0,
                LanguageCode = "ar"
            };

            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();

            // 2. إضافة الترجمة
            //var translation = new ProductTranslation
            //{
            //   // Id = product.Id,
            //    ProductId = product.Id,
            //    Name = productDto.NameEn,
            //    Description = productDto.FullDescriptionEn,
            //    ShortDescriptionEn = productDto.ShortDescriptionEn,
            //    SKU = product.SKU,
            //    AverageRate = 0,
            //    Price = product.Price,
            //    PriceAfterDiscount = product.PriceAfterDiscount
            //};
            //ctx.ProductTranslations.Add(translation);

            // 3. إضافة الألوان
            if (productDto.Colors != null && productDto.Colors.Any())
            {
                foreach (var color in productDto.Colors)
                {
                    var colorId = await GetOrCreateColorId(color.NameEn, color.NameAr);

                    await ctx.NewProductColors.AddAsync(new NewProductColor
                    {
                        ProductId = product.Id,
                        ColorId = colorId
                    });
                }
            }

            // 4. إضافة الـ Variants بالأسعار المختلفة
            if (productDto.Variants != null && productDto.Variants.Any())
            {
                foreach (var variantDto in productDto.Variants)
                {
                    // حساب نسبة الخصم
                    decimal discountRate = 0;
                    if (variantDto.Price > 0)
                    {
                        var discount = variantDto.Price - variantDto.PriceAfterDiscount;
                        discountRate = Math.Round((discount / variantDto.Price) * 100, 2);
                    }

                    // Variant عربي
                    ctx.Variants.Add(new Variant
                    {
                        ProductId = product.Id,
                        SKU = productDto.SKU,
                        Size = variantDto.Size.SizeAr,

                        EnglishSize = variantDto.Size.SizeEn,
                        Type = variantDto.Type.TypeAr,
                        EnglishType = variantDto.Type.TypeEn,
                        Price = variantDto.PriceAfterDiscount,  // السعر النهائي
                        PriceBeforeDiscount = variantDto.Price,  // السعر قبل الخصم
                        DiscountRate = discountRate,
                        LanguageCode = "ar"
                    });

                    //// Variant إنجليزي
                    //ctx.Variants.Add(new Variant
                    //{
                    //    ProductId = product.Id,
                    //    SKU = productDto.SKU,
                    //    Size = variantDto.Size.SizeEn,
                    //    EnglishSize = variantDto.Size.SizeEn,
                    //    Type = variantDto.Type.TypeEn,
                    //    EnglishType = variantDto.Type.TypeEn,
                    //    Price = variantDto.PriceAfterDiscount,  // السعر النهائي
                    //    PriceBeforeDiscount = variantDto.Price,  // السعر قبل الخصم
                    //    DiscountRate = discountRate,
                    //    LanguageCode = "en"
                    //});
                }
            }

            // 5. إضافة الصور
            if (images != null && images.Any())
            {
                foreach (var file in images)
                {
                    var imageUrl = await SaveImageFileOFAdd(file, "products", env, config);
                    ctx.Images.Add(new Image
                    {
                        ProductId = product.Id,
                        RelativePath = imageUrl
                    });
                }
            }

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error adding product: {ex.InnerException?.Message ?? ex.Message}");
            return false;
        }
    }

    // Helper Method
    private async Task<int> GetOrCreateColorId(string colorEN, string colorAR)
    {
        var existingColor = await ctx.Colors
            .FirstOrDefaultAsync(c =>
                c.EnglishColor.ToLower() == colorEN.ToLower() ||
                c.Color.ToLower() == colorAR.ToLower());

        if (existingColor != null)
            return existingColor.Id;

        var newColor = new Color
        {
            ColorName = colorAR,
            EnglishColor = colorEN
        };

        ctx.ColorEntities.Add(newColor);
        await ctx.SaveChangesAsync();

        return newColor.Id;
    }

    private async Task<string> SaveImageFileOFAdd(IFormFile file, string folder, IWebHostEnvironment env, IConfiguration config)
    {
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var uploadPath = Path.Combine(env.WebRootPath, "images", folder);

        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var baseUrl = config["ImageSettings:BaseUrl"];
        return $"{baseUrl}/images/{folder}/{fileName}";
    }

    public async Task<bool> UpdateProduct(
    int productId,
    ProductUpdateDto productDto,
    List<IFormFile>? newImages,
    List<int>? deleteImageIds,
    IWebHostEnvironment env,
    IConfiguration config)
    {
        using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var product = await ctx.Products
                .Include(p => p.NewProductColors).ThenInclude(pc => pc.Color)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return false;

            var category = await ctx.CategoryImages
                .FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == productDto.CategoryValue.ToLower());

            if (category == null)
                return false;

            decimal basePrice = productDto.Price;
            decimal basePriceAfterDiscount = productDto.PriceAfterDiscount;

            if (basePrice == 0 && productDto.Variants != null && productDto.Variants.Any())
            {
                var firstVariant = productDto.Variants.First();
                basePrice = firstVariant.Price;
                basePriceAfterDiscount = /*firstVariant.Price - */firstVariant.PriceAfterDiscount;
            }

            product.Name = productDto.NameAr?? product.Name;
            product.NameEN = productDto.NameEn?? product.NameEN;
            product.Descriptions = productDto.FullDescriptionAr?? product.Descriptions;
            product.DescriptionsEN = productDto.FullDescriptionEn?? product.DescriptionsEN;
            product.ShortDescriptionEn = productDto.ShortDescriptionEn?? product.ShortDescriptionEn;
            product.FullDescription = productDto.ShortDescriptionAr?? product.FullDescription;
            product.Price = basePrice==0? product.Price : basePrice;
            product.PriceAfterDiscount = basePriceAfterDiscount==0? product.PriceAfterDiscount : basePriceAfterDiscount;
            product.SKU = productDto.SKU?? product.SKU;
            product.CategoryFK = category.Id;
            product.LastUpdate = DateTime.UtcNow;


            if (productDto.Colors != null)
            {
                var oldColors = product.NewProductColors.ToList();
                ctx.NewProductColors.RemoveRange(oldColors);

                foreach (var color in productDto.Colors)
                {
                    var colorId = await GetOrCreateColorId(color.NameEn, color.NameAr);

                    await ctx.NewProductColors.AddAsync(new NewProductColor
                    {
                        ProductId = product.Id,
                        ColorId = colorId
                    });
                }
            }

            if (productDto.Variants != null)
            {
                var oldVariants = product.Variants.ToList();
                ctx.Variants.RemoveRange(oldVariants);

                foreach (var variantDto in productDto.Variants)
                {
                    decimal discountRate = 0;
                    if (variantDto.Price > 0)
                    {
                        var discount = variantDto.Price - variantDto.PriceAfterDiscount;
                        discountRate = Math.Round((discount / variantDto.Price) * 100, 2);
                    }

                    ctx.Variants.Add(new Variant
                    {
                        ProductId = product.Id,
                        SKU = productDto.SKU,
                        Size = variantDto.Size.SizeAr,
                        EnglishSize = variantDto.Size.SizeEn,
                        Type = variantDto.Type.TypeAr,
                        EnglishType = variantDto.Type.TypeEn,
                        Price = variantDto.PriceAfterDiscount,
                        PriceBeforeDiscount = variantDto.Price,
                        DiscountRate = discountRate,
                        LanguageCode = "ar"
                    });
                }
            }

            if (deleteImageIds != null && deleteImageIds.Any())
            {
                foreach (var imageId in deleteImageIds)
                {
                    var image = product.Images.FirstOrDefault(i => i.Id == imageId);
                    if (image != null)
                    {
                        if (!string.IsNullOrEmpty(image.RelativePath) && !image.RelativePath.StartsWith("http"))
                        {
                            try
                            {
                                var fileName = Path.GetFileName(new Uri(image.RelativePath).LocalPath);
                                var imagePath = Path.Combine(env.WebRootPath, "images", "products", fileName);

                                if (File.Exists(imagePath))
                                {
                                    File.Delete(imagePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error deleting image: {ex.Message}");
                            }
                        }

                        ctx.Images.Remove(image);
                    }
                }
            }

            if (newImages != null && newImages.Any())
            {
                foreach (var file in newImages)
                {
                    var imageUrl = await SaveImageFileOFAdd(file, "products", env, config);
                    ctx.Images.Add(new Image
                    {
                        ProductId = product.Id,
                        RelativePath = imageUrl
                    });
                }
            }

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error updating product: {ex.InnerException?.Message ?? ex.Message}");
            return false;
        }
    }

    // Helper Methods (نفسها من Add)
    //private async Task<int> GetOrCreateColorId(string colorEN, string colorAR)
    //{
    //    var existingColor = await ctx.Colors
    //        .FirstOrDefaultAsync(c =>
    //            c.EnglishColor.ToLower() == colorEN.ToLower() ||
    //            c.Color.ToLower() == colorAR.ToLower());

    //    if (existingColor != null)
    //        return existingColor.Id;

    //    var newColor = new Color
    //    {
    //        ColorName = colorAR,
    //        EnglishColor = colorEN
    //    };

    //    ctx.ColorEntities.Add(newColor);
    //    await ctx.SaveChangesAsync();

    //    return newColor.Id;
    //}

    //private async Task<string> SaveImageFileOFAdd(IFormFile file, string folder, IWebHostEnvironment env, IConfiguration config)
    //{
    //    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
    //    var uploadPath = Path.Combine(env.WebRootPath, "images", folder);

    //    if (!Directory.Exists(uploadPath))
    //        Directory.CreateDirectory(uploadPath);

    //    var filePath = Path.Combine(uploadPath, fileName);
    //    using (var stream = new FileStream(filePath, FileMode.Create))
    //    {
    //        await file.CopyToAsync(stream);
    //    }

    //    var baseUrl = config["ImageSettings:BaseUrl"];
    //    return $"{baseUrl}/images/{folder}/{fileName}";
    //}

    //======
    //public async Task<bool> AddProduct(ProductAddDtoaren productDto, IWebHostEnvironment env, IConfiguration config)
    //{
    //    using var transaction = await ctx.Database.BeginTransactionAsync();
    //    try
    //    {
    //        var category = await ctx.CategoryImages
    //            .FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == productDto.CategoryValue.ToLower());

    //        if (category == null)
    //            return false;

    //        var priceAfterDiscount = productDto.PriceBeforeDiscount.HasValue
    //            ? productDto.PriceBeforeDiscount.Value - productDto.Price
    //            : 0;

    //         var product = new Product
    //        {
    //            Name = productDto.NameAR,
    //            NameEN = productDto.NameEN,
    //            Descriptions = productDto.DescriptionAR,
    //            DescriptionsEN = productDto.DescriptionEN,
    //            FullDescription = productDto.DescriptionAR,
    //            Price = productDto.Price,
    //            PriceAfterDiscount = priceAfterDiscount,
    //            SKU = productDto.SKU,
    //            CategoryFK = category.Id,
    //            Created = DateTime.UtcNow,
    //            AverageRate = 0,
    //            TotalRatePeople = 0,
    //            RatingSum = 0,
    //            LanguageCode = "ar"
    //        };

    //        ctx.Products.Add(product);
    //        await ctx.SaveChangesAsync();

    //         var translation = new ProductTranslation
    //        {
    //            Id=product.Id,
    //            ProductId = product.Id,
    //            Name = productDto.NameEN,
    //            Description = productDto.DescriptionEN,
    //            SKU=product.SKU,
    //            AverageRate = 0,
    //           Price=product.Price,
    //           PriceAfterDiscount=product.PriceAfterDiscount

    //        };
    //        ctx.ProductTranslations.Add(translation);

    //         if (productDto.ColorsAR != null && productDto.ColorsAR.Any())
    //        {
    //            for (int i = 0; i < productDto.ColorsAR.Count; i++)
    //            {
    //                // ctx.Colors.Add(new ProductColor
    //                //{
    //                //    ProductId = product.Id,
    //                //    Color = productDto.ColorsAR[i],
    //                //    LanguageCode = "ar"
    //                //});

    //                // if (productDto.ColorsEN != null && i < productDto.ColorsEN.Count)
    //                //{
    //                //    ctx.Colors.Add(new ProductColor
    //                //    {
    //                //        ProductId = product.Id,
    //                //        Color = productDto.ColorsEN[i],
    //                //        LanguageCode = "en"
    //                //    });
    //                //}
    //          await ctx.NewProductColors.AddAsync(new NewProductColor
    //                {
    //                    ProductId = product.Id,
    //                    ColorId = await GetOrCreateColorId(productDto.ColorsEN[i],productDto.ColorsAR[i]),
    //                });


    //            }
    //        }

    //         if (productDto.SizesAR != null && productDto.SizesAR.Any())
    //        {
    //            var typesAR = productDto.TypesAR != null && productDto.TypesAR.Any()
    //                ? productDto.TypesAR
    //                : new List<string> { "" };

    //            var typesEN = productDto.TypesEN != null && productDto.TypesEN.Any()
    //                ? productDto.TypesEN
    //                : new List<string> { "" };

    //            for (int i = 0; i < productDto.SizesAR.Count; i++)
    //            {
    //                var sizeAR = productDto.SizesAR[i];
    //                var sizeEN = productDto.SizesEN != null && i < productDto.SizesEN.Count
    //                    ? productDto.SizesEN[i]
    //                    : sizeAR;

    //                for (int j = 0; j < typesAR.Count; j++)
    //                {
    //                    var typeAR = typesAR[j];
    //                    var typeEN = j < typesEN.Count ? typesEN[j] : typeAR;

    //                    decimal discountRate = 0;
    //                    if (productDto.PriceBeforeDiscount.HasValue && productDto.PriceBeforeDiscount.Value > 0)
    //                    {
    //                        discountRate = Math.Round(
    //                            ((productDto.PriceBeforeDiscount.Value - productDto.Price) / productDto.PriceBeforeDiscount.Value) * 100,
    //                            2
    //                        );
    //                    }

    //                     ctx.Variants.Add(new Variant
    //                    {
    //                        ProductId = product.Id,
    //                        SKU = productDto.SKU,
    //                        Size = sizeAR,
    //                        EnglishSize = sizeEN,
    //                        Type = typeAR,
    //                        EnglishType = typeEN,
    //                        Price = productDto.Price,
    //                        PriceBeforeDiscount = productDto.PriceBeforeDiscount,
    //                        DiscountRate = discountRate,
    //                        LanguageCode = "ar",
    //                   //     Quantity = 0
    //                    });

    //                     ctx.Variants.Add(new Variant
    //                    {
    //                        ProductId = product.Id,
    //                        SKU = productDto.SKU,
    //                        Size = sizeEN,
    //                        EnglishSize = sizeEN,
    //                        Type = typeEN,
    //                        EnglishType = typeEN,
    //                        Price = productDto.Price,
    //                        PriceBeforeDiscount = productDto.PriceBeforeDiscount,
    //                        DiscountRate = discountRate,
    //                        LanguageCode = "en",
    //                   //     Quantity = 0
    //                    });
    //                }
    //            }
    //        }

    //         if (productDto.ImageFiles != null && productDto.ImageFiles.Any())
    //        {
    //            foreach (var file in productDto.ImageFiles)
    //            {
    //                var imageUrl = await SaveImageFile(file, "products", env, config);
    //                ctx.Images.Add(new Image
    //                {
    //                    ProductId = product.Id,
    //                    RelativePath = imageUrl
    //                });
    //            }
    //        }
    //        else if (productDto.ImageUrls != null && productDto.ImageUrls.Any())
    //        {
    //            foreach (var url in productDto.ImageUrls)
    //            {
    //                ctx.Images.Add(new Image
    //                {
    //                    ProductId = product.Id,
    //                    RelativePath = url
    //                });
    //            }
    //        }

    //        await ctx.SaveChangesAsync();
    //        await transaction.CommitAsync();

    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        await transaction.RollbackAsync();
    //        Console.WriteLine($"Error adding product: {ex.InnerException}");
    //        return false;
    //    }
    //}

    //private async Task<int> GetOrCreateColorId(string englishColor,string colorName)
    //{
    //    var color = await ctx.ColorEntities.FirstOrDefaultAsync(c =>( c.EnglishColor.ToLower() == englishColor.ToLower()|| c.ColorName.ToLower() == colorName.ToLower()));
    //    if (color != null)
    //    {
    //        return color.Id;
    //    }
    //    else
    //    {
    //        var newColor = new Color
    //        {
    //            EnglishColor = englishColor,
    //            ColorName = colorName
    //        };
    //        ctx.ColorEntities.Add(newColor);
    //        await ctx.SaveChangesAsync();
    //        return newColor.Id;
    //    }
    //}
    ////=========
    //public async Task<bool> UpdateProduct(int productId, ProductUpdateDto productDto, IWebHostEnvironment env, IConfiguration config)
    //{
    //    using var transaction = await ctx.Database.BeginTransactionAsync();
    //    try
    //    {
    //         var product = await ctx.Products
    //            //.Include(p => p.ProductTranslations)
    //           // .Include(p => p.Colors)
    //            .Include(p => p.NewProductColors)
    //            .Include(p => p.Variants)
    //            .Include(p => p.Images)
    //            .FirstOrDefaultAsync(p => p.Id == productId);

    //        if (product == null)
    //            return false;

    //         var category = await ctx.CategoryImages
    //            .FirstOrDefaultAsync(c => c.CategoryValue.ToLower() == productDto.CategoryValue.ToLower());

    //        if (category == null)
    //            return false;

    //        var priceAfterDiscount = productDto.PriceBeforeDiscount.HasValue
    //            ? productDto.PriceBeforeDiscount.Value - productDto.Price
    //            : 0;

    //         product.Name = productDto.NameAR;
    //        product.NameEN = productDto.NameEN;
    //        product.Descriptions = productDto.DescriptionAR;
    //        product.DescriptionsEN = productDto.DescriptionEN;
    //        product.FullDescription = productDto.fullDescriptionAR;
    //        product.Price = productDto.Price;
    //        product.PriceAfterDiscount = priceAfterDiscount;
    //        product.SKU = productDto.SKU;
    //        product.CategoryFK = category.Id;

    //        // var translation = await ctx.ProductTranslations.FirstOrDefaultAsync(t => t.Id == product.Id);
    //        //if (translation != null)
    //        //{
    //        //    translation.Name = productDto.NameEN;
    //        //    translation.Description = productDto.DescriptionEN;
    //        //    translation.Price = productDto.Price;
    //        //    translation.PriceAfterDiscount = priceAfterDiscount;
    //        //    translation.SKU = productDto.SKU;
    //        //}
    //        //else
    //        //{
    //        //    ctx.ProductTranslations.Add(new ProductTranslation
    //        //    {
    //        //        Id = product.Id,
    //        //        ProductId = product.Id,
    //        //        Name = productDto.NameEN,
    //        //        Description = productDto.DescriptionEN,
    //        //        SKU = productDto.SKU,
    //        //        AverageRate = 0,
    //        //        Price = productDto.Price,
    //        //        PriceAfterDiscount = priceAfterDiscount
    //        //    });
    //        //}

    //         if (productDto.ColorsAR != null || productDto.ColorsEN != null)
    //        {
    //           //  var oldColors = product.Colors.ToList();
    //             var oldColors = product.NewProductColors.Select(c=>c.Color).ToList();
    //         //   ctx.Colors.RemoveRange(oldColors);
    //            ctx.ColorEntities.RemoveRange(oldColors);

    //             if (productDto.ColorsAR != null && productDto.ColorsAR.Any())
    //            {
    //                for (int i = 0; i < productDto.ColorsAR.Count; i++)
    //                {
    //                    ctx.Colors.Add(new ProductColor
    //                    {
    //                        ProductId = product.Id,
    //                        Color = productDto.ColorsAR[i],
    //                        LanguageCode = "ar"
    //                    });

    //                    if (productDto.ColorsEN != null && i < productDto.ColorsEN.Count)
    //                    {
    //                        ctx.Colors.Add(new ProductColor
    //                        {
    //                            ProductId = product.Id,
    //                            Color = productDto.ColorsEN[i],
    //                            LanguageCode = "en"
    //                        });
    //                    }
    //                }
    //            }
    //        }

    //         if (productDto.SizesAR != null || productDto.SizesEN != null)
    //        {
    //             var oldVariants = product.Variants.ToList();
    //            ctx.Variants.RemoveRange(oldVariants);

    //             if (productDto.SizesAR != null && productDto.SizesAR.Any())
    //            {
    //                var typesAR = productDto.TypesAR != null && productDto.TypesAR.Any()
    //                    ? productDto.TypesAR
    //                    : new List<string> { "" };

    //                var typesEN = productDto.TypesEN != null && productDto.TypesEN.Any()
    //                    ? productDto.TypesEN
    //                    : new List<string> { "" };

    //                for (int i = 0; i < productDto.SizesAR.Count; i++)
    //                {
    //                    var sizeAR = productDto.SizesAR[i];
    //                    var sizeEN = productDto.SizesEN != null && i < productDto.SizesEN.Count
    //                        ? productDto.SizesEN[i]
    //                        : sizeAR;

    //                    for (int j = 0; j < typesAR.Count; j++)
    //                    {
    //                        var typeAR = typesAR[j];
    //                        var typeEN = j < typesEN.Count ? typesEN[j] : typeAR;

    //                        decimal discountRate = 0;
    //                        if (productDto.PriceBeforeDiscount.HasValue && productDto.PriceBeforeDiscount.Value > 0)
    //                        {
    //                            discountRate = Math.Round(
    //                                ((productDto.PriceBeforeDiscount.Value - productDto.Price) / productDto.PriceBeforeDiscount.Value) * 100,
    //                                2
    //                            );
    //                        }

    //                         ctx.Variants.Add(new Variant
    //                        {
    //                            ProductId = product.Id,
    //                            SKU = productDto.SKU,
    //                            Size = sizeAR,
    //                            EnglishSize = sizeEN,
    //                            Type = typeAR,
    //                            EnglishType = typeEN,
    //                            Price = productDto.Price,
    //                            PriceBeforeDiscount = productDto.PriceBeforeDiscount,
    //                            DiscountRate = discountRate,
    //                            LanguageCode = "ar"
    //                        });

    //                         ctx.Variants.Add(new Variant
    //                        {
    //                            ProductId = product.Id,
    //                            SKU = productDto.SKU,
    //                            Size = sizeEN,
    //                            EnglishSize = sizeEN,
    //                            Type = typeEN,
    //                            EnglishType = typeEN,
    //                            Price = productDto.Price,
    //                            PriceBeforeDiscount = productDto.PriceBeforeDiscount,
    //                            DiscountRate = discountRate,
    //                            LanguageCode = "en"
    //                        });
    //                    }
    //                }
    //            }
    //        }

    //         if (productDto.DeleteImageIds != null && productDto.DeleteImageIds.Any())
    //        {
    //            foreach (var imageId in productDto.DeleteImageIds)
    //            {
    //                var image = product.Images.FirstOrDefault(i => i.Id == imageId);
    //                if (image != null)
    //                {
    //                     if (!string.IsNullOrEmpty(image.RelativePath) && !image.RelativePath.StartsWith("http"))
    //                    {
    //                        try
    //                        {
    //                            var imagePath = Path.Combine(env.WebRootPath, image.RelativePath.TrimStart('/'));
    //                            if (File.Exists(imagePath))
    //                            {
    //                                File.Delete(imagePath);
    //                            }
    //                        }
    //                        catch (Exception ex)
    //                        {
    //                            Console.WriteLine($"Error deleting image: {ex.Message}");
    //                        }
    //                    }

    //                    ctx.Images.Remove(image);
    //                }
    //            }
    //        }

    //        if (productDto.NewImageFiles != null && productDto.NewImageFiles.Any())
    //        {
    //            foreach (var file in productDto.NewImageFiles)
    //            {
    //                var imageUrl = await SaveImageFile(file, "products", env, config);
    //                ctx.Images.Add(new Image
    //                {
    //                    ProductId = product.Id,
    //                    RelativePath = imageUrl
    //                });
    //            }
    //        }
    //        else if (productDto.NewImageUrls != null && productDto.NewImageUrls.Any())
    //        {
    //            foreach (var url in productDto.NewImageUrls)
    //            {
    //                ctx.Images.Add(new Image
    //                {
    //                    ProductId = product.Id,
    //                    RelativePath = url
    //                });
    //            }
    //        }

    //        await ctx.SaveChangesAsync();
    //        await transaction.CommitAsync();

    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        await transaction.RollbackAsync();
    //        Console.WriteLine($"Error updating product: {ex.InnerException}");
    //        return false;
    //    }
    //}
    /// <summary>
    /// حذف منتج بالكامل (مع جميع البيانات المرتبطة)
    /// </summary>
    /// 
    public async Task<(bool Success, string Message)> DeleteProduct(int productId, IWebHostEnvironment env)
    {
        using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var product = await ctx.Products
                .Include(p => p.Images)
                .Include(p => p.NewProductColors)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return (false, "المنتج غير موجود");

            // ✅ حذف الصور من السيرفر
            if (product.Images != null && product.Images.Any())
            {
                foreach (var image in product.Images)
                {
                    if (!string.IsNullOrEmpty(image.RelativePath) && !image.RelativePath.StartsWith("http"))
                    {
                        try
                        {
                            var cleanPath = image.RelativePath.Replace("/", Path.DirectorySeparatorChar.ToString());
                            var imagePath = Path.Combine(env.WebRootPath, cleanPath);

                            var fullPath = Path.GetFullPath(imagePath);

                            if (!fullPath.StartsWith(env.WebRootPath))
                                continue;

                            if (File.Exists(fullPath))
                            {
                                File.Delete(fullPath);
                                Console.WriteLine($"Deleted Image: {image.RelativePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting image: {ex.Message}");
                        }
                    }
                }
            }

            // ✅ حذف العلاقات (لو Cascade مش مضمون)
            ctx.Images.RemoveRange(product.Images);
            ctx.NewProductColors.RemoveRange(product.NewProductColors);
            ctx.Variants.RemoveRange(product.Variants);
            ctx.Reviews.RemoveRange(product.Reviews);

            ctx.Products.Remove(product);

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "تم حذف المنتج بنجاح");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error deleting product: {ex.Message}");
            return (false, $"حدث خطأ أثناء حذف المنتج");
        }
    }

    //public async Task<(bool Success, string Message)> DeleteProduct(int productId, IWebHostEnvironment env)
    //{
    //    using var transaction = await ctx.Database.BeginTransactionAsync();
    //    try
    //    {
    //        var product = await ctx.Products
    //            .Include(p => p.Images)
    //            //    .Include(p => p.ProductTranslations)
    //            //   .Include(p => p.Colors)
    //            .Include(p => p.NewProductColors)
    //            .Include(p => p.Variants)
    //            .Include(p => p.Reviews)
    //            .FirstOrDefaultAsync(p => p.Id == productId);

    //        if (product == null)
    //            return (false, "المنتج غير موجود");

    //        // حذف الصور من السيرفر
    //        if (product.Images != null && product.Images.Any())
    //        {
    //            foreach (var image in product.Images)
    //            {
    //                if (!string.IsNullOrEmpty(image.RelativePath) && !image.RelativePath.StartsWith("http"))
    //                {
    //                    try
    //                    {
    //                        var imagePath = Path.Combine(env.WebRootPath, image.RelativePath.TrimStart('/'));
    //                        if (File.Exists(imagePath))
    //                        {
    //                            File.Delete(imagePath);
    //                        }
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        Console.WriteLine($"Error deleting image: {ex.Message}");
    //                    }
    //                }
    //            }
    //        }
    //        //var translations = await ctx.ProductTranslations
    //        //    .FirstOrDefaultAsync(t => t.Id == productId);

    //        // حذف المنتج (cascade delete للباقي)
    //        ctx.Products.Remove(product);
    //        //ctx.ProductTranslations.Remove(translations);
    //        await ctx.SaveChangesAsync();
    //        await transaction.CommitAsync();

    //        return (true, "تم حذف المنتج بنجاح");
    //    }
    //    catch (Exception ex)
    //    {
    //        await transaction.RollbackAsync();
    //        Console.WriteLine($"Error deleting product: {ex.Message}");
    //        return (false, $"حدث خطأ أثناء حذف المنتج: {ex.Message}");
    //    }
    //}
    private async Task<string> SaveImageFile(IFormFile file, string folder, IWebHostEnvironment env, IConfiguration config)
    {
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var uploadPath = Path.Combine(env.WebRootPath, "images", folder);

        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var baseUrl = config["ImageSettings:BaseUrl"];
        return $"{baseUrl}/images/{folder}/{fileName}";
    }


    public bool DeleteProduct(int id)
    {
        try
        {
            var product = ctx.Products.Find(id);

            if (product == null)
                return false;

            ctx.Products.Remove(product);

            return ctx.SaveChanges() > 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

   
    
    
    
    public async Task<List<ImageFileResult>> GetImagesByProductIdAsync(int productId, IWebHostEnvironment _env)
    {
       
        var imageRecords = await ctx.Images
            .Where(a => a.ProductId == productId)
            .ToListAsync();

        var results = new List<ImageFileResult>();

        
        foreach (var image in imageRecords)
        {
            
            results.Add(new ImageFileResult
            {
                FullPath = image.RelativePath,
                ContentType = "image/" + Path.GetExtension(image.RelativePath).TrimStart('.'),
                FileName = Path.GetFileName(image.RelativePath)
            });
        }

        return results; 
    }



    //////////////////////////
    ///
    // دالة مساعدة (Helper Method)
    private async Task<List<int>> GetWishlistIdsAsync(string? userId, List<int>? frontendIds)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            return await ctx.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();
        }

        if (frontendIds != null && frontendIds.Any())
        {
            return frontendIds;
        }

        return new List<int>();
    }

    public async Task<int> GetTotalProductsCount()
    {
        return await ctx.Products.CountAsync();

    }
}

