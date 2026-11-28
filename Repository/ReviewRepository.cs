using Microsoft.EntityFrameworkCore;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.Entity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WallsShop.Repository;

public class ReviewRepository(WallShopContext ctx)
{
    [Authorize]
    //public async  Task<bool> CreateReview(ReviewDto review , ClaimsPrincipal  User,  UserManager<User> _userManager)
    //{
    //    var user = await _userManager.GetUserAsync(User);

    //    var product = ctx.Products.Find(review.ProductId);
    //    if (product ==null)
    //        return false;
    //    try
    //    {
    //        product.TotalRatePeople += 1;
    //        product.RatingSum += review.Rating;
    //        if (product.TotalRatePeople > 0)
    //        {
    //            product.AverageRate = product.RatingSum / product.TotalRatePeople;
    //        }
    //        else
    //        {
    //            product.AverageRate = 0;
    //        }
    //        Review newReview = new Review()
    //        {
    //            ProductId = review.ProductId,
    //            UserFK = user.Id,
    //            Comment = review.Comment,
    //            UserName =   user.Name,
    //            ReviewDate = DateTime.Now,
    //            Rate = review.Rating 
    //        };
    //        ctx.Reviews.Add(newReview);
    //        await ctx.SaveChangesAsync();
    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        return false;
    //    }
    //}
    public async Task<ReviewResponseDto> CreateReview(ReviewDto review, ClaimsPrincipal User, UserManager<User> _userManager)
    {
        var user = await _userManager.GetUserAsync(User);
        bool isadmin = await _userManager.IsInRoleAsync(user, "Admin");
        var product = ctx.Products.Find(review.ProductId);
        if (product == null)
            return null;
        try
        {
            product.TotalRatePeople += 1;
            product.RatingSum += review.Rating;
            if (product.TotalRatePeople > 0)
            {
                product.AverageRate = product.RatingSum / product.TotalRatePeople;
            }
            else
            {
                product.AverageRate = 0;
            }
            Review newReview = new Review()
            {
                ProductId = review.ProductId,
                UserFK = user.Id,
                Comment = review.Comment,
                UserName = user.Name,
                ReviewDate = DateTime.Now,
                Rate = review.Rating
            };
            ctx.Reviews.Add(newReview);
            await ctx.SaveChangesAsync();
            var res= await GetProductReviews(review.ProductId, user.Id, isadmin);
            return res;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<(DeleteReviewStatus Status, ReviewResponseDto? Response)> DeleteReview(int id, ClaimsPrincipal User, UserManager<User> _userManager)
    {

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return (DeleteReviewStatus.Forbidden, null);

        bool isadmin = await _userManager.IsInRoleAsync(user, "Admin");
        var review = await ctx.Reviews.FindAsync(id);
            
        if (review == null)
            return (DeleteReviewStatus.NotFound, null);

        if (review.UserFK != user.Id && !isadmin)
            return (DeleteReviewStatus.Forbidden, null);

        try
        {
            var product = await ctx.Products.Where(a => a.Id == review.ProductId).FirstOrDefaultAsync();


            if (product == null)
                return (DeleteReviewStatus.NotFound, null);

            product.TotalRatePeople -= 1;
            product.RatingSum -= review.Rate;
            product.AverageRate = product.TotalRatePeople > 0 ? product.RatingSum / product.TotalRatePeople : 0;
            
            ctx.Reviews.Remove(review);
            await ctx.SaveChangesAsync();
            var res = await GetProductReviews(review.ProductId, user.Id, isadmin);
            return (DeleteReviewStatus.Success, res);
        }
        catch (Exception ex)
        {
            return (DeleteReviewStatus.Error, null);
        }
    }
    // 3.2 out of 132 person 

    // fayez : 

    // adham : 
    //public async Task<ReviewResponseDto> GetProductReviews(int productId, string userId, bool isAdmin)
    //{
    //    return await ctx
    //        .Products
    //        .Include(a => a.Reviews)
    //        .Where(a => a.Id == productId)?.
    //        Select(a => new ReviewResponseDto()
    //        {
    //            ProductId = a.Id,
    //            AverageRating = a.AverageRate,
    //            TotalReviews = a.TotalRatePeople,
    //            SingleReviews = a.Reviews.Select(a => new SingleReviewDto()
    //            {
    //                UserName = a.UserName,
    //                Comment = a.Comment,
    //                Date = a.ReviewDate,
    //                CanBeDeleted = userId == a.UserName || isAdmin ? true : false,
    //                Rate = a.Rate
    //            }).ToList()
    //        }).FirstOrDefaultAsync();
    //}
    public async Task<ReviewResponseDto> GetProductReviews(int productId, string userId, bool isAdmin)
    {
        return await ctx
            .Products
            .Include(a => a.Reviews)
            .ThenInclude(r => r.User)
            .Where(a => a.Id == productId)
            .Select(a => new ReviewResponseDto()
            {
                ProductId = a.Id,
                AverageRating = a.AverageRate,
                TotalReviews = a.TotalRatePeople,
                SingleReviews = a.Reviews.Select(r => new SingleReviewDto()
                {
                    Id= r.Id,
                    UserName = r.User.Name,
                    Comment = r.Comment,
                    Date = r.ReviewDate,
                    CanBeDeleted = (r.UserFK == userId || isAdmin) ? true : false,
                //     CanBeDeleted =isAdmin?true:false, //userId == r.UserName || isAdmin, // Removed ternary
                    Rate = r.Rate
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<ReviewResponseDto2>> GetAllReviewsWithDetailsAsync(string LanguageCode)
    {
        bool isArabic = LanguageCode.Equals("ar", StringComparison.OrdinalIgnoreCase);

        var query = from r in ctx.Reviews
                    join u in ctx.Users on r.UserFK equals u.Id
                    join p in ctx.Products on r.ProductId equals p.Id
                    orderby r.ReviewDate descending
                    select new ReviewResponseDto2
                    {
                        Id = r.Id,
                        Comment = r.Comment,
                        UserName = r.UserName,
                        Email = u.Email,
                        PhoneNumber=u.PhoneNumber,
                        ProductId = p.Id,
                        ProductName = isArabic ? p.Name : p.NameEN,
                        ReviewDate = r.ReviewDate,
                        Rate = r.Rate
                    };

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<List<PublicReviewDto>> GetTop10ReviewsAsync(string languageCode = "en")
    {
        bool isArabic = languageCode.Equals("ar", StringComparison.OrdinalIgnoreCase);

        var query = from r in ctx.Reviews
                    join u in ctx.Users on r.UserFK equals u.Id
                    join p in ctx.Products on r.ProductId equals p.Id
                    orderby r.Rate descending, r.ReviewDate descending
                    select new PublicReviewDto
                    {
                        Id = r.Id,
                        UserName = u.Name ?? r.UserName,
                        Comment = r.Comment,
                        ProductId = p.Id,
                        ProductName = isArabic ? p.Name : p.NameEN,
                        ProductPrice = p.Price,
                        ProductPriceAfterDiscount = p.PriceAfterDiscount,
                        ProductImageUrl = p.Images.Select(i => i.RelativePath).FirstOrDefault(),
                        ReviewDate = r.ReviewDate,
                        Rate = r.Rate
                    };

        return await query.AsNoTracking().Take(10).ToListAsync();
    }
}

public enum DeleteReviewStatus
{
    Success,
    NotFound,
    Forbidden,
    Error
}
