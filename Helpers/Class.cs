
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;
using WallsShop.Entity;

namespace WallsShop.Helpers
{
    public class BlockedUserMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockedUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<User> userManager)
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                await WriteJsonErrorResponse(context, "Invalid Token", 401);
                return;
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                await WriteJsonErrorResponse(context, "User Not Found", 401);
                return;
            }

            if (user.IsBlocked)
            {
                await WriteJsonErrorResponse(context, "User Is Blocked", 401);
                return;
            }

            await _next(context);
        }

        // 👇 Method مساعدة لردود الأخطاء (انسخوا الكود ده)
        private static async Task WriteJsonErrorResponse(HttpContext context, string message, int statusCode)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json"; // 👈 دي الأهم

            var response = new
            {
                success = false,
                message = message,        // 👈 الرسالة اللي هقراها في Angular
                statusCode = statusCode,
                error = "Unauthorized"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}













//using Microsoft.AspNetCore.Identity;
//using System.Security.Claims;
//using WallsShop.Entity;

//namespace WallsShop.Helpers
//{
//    public class BlockedUserMiddleware
//    {
//        private readonly RequestDelegate _next;

//        public BlockedUserMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task InvokeAsync(
//            HttpContext context,
//            UserManager<User> userManager)
//        {
//            if (!(context.User.Identity?.IsAuthenticated ?? false))
//            {
//                await _next(context);
//                return;
//            }

//            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                await context.Response.WriteAsync("Invalid Token");
//                return;
//            }

//            var user = await userManager.FindByIdAsync(userId);

//            if (user == null)
//            {
//                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                await context.Response.WriteAsync("User Not Found");
//                return;
//            }

//            if (user.IsBlocked)
//            {
//                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                await context.Response.WriteAsync("User Is Blocked");
//                return;
//            }

//            await _next(context);
//        }
//    }
//}









////using Microsoft.AspNetCore.Identity;
////using System.Security.Claims;
////using WallsShop.Entity;

////namespace WallsShop.Helpers
////{
////    public class BlockedUserMiddleware
////    {
////        private readonly RequestDelegate _next;

////        public BlockedUserMiddleware(RequestDelegate next)
////        {
////            _next = next;
////        }

////        public async Task<string> InvokeAsync(
////            HttpContext context,
////            UserManager<User> userManager)
////        {
////            // لو مش Authenticated → عدّي
////            if (!context.User.Identity?.IsAuthenticated ?? true)
////            {
////                await _next(context);
////                return string.Empty;
////            }

////            // لو Authenticated → نراجع
////            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

////            if (userId != null)
////            {
////                var user = await userManager.FindByIdAsync(userId);

////                if (user?.IsBlocked == true)
////                {
////                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
////                    return "Token Is Not Valid";
////                }
////            }

////            await _next(context);
////            return string.Empty;
////        }
////    }

////}

