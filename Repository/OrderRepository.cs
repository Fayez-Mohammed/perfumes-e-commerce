using Microsoft.EntityFrameworkCore;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.Properties.Entity;
using WallsShop.Services;

namespace WallsShop.Repository;

public class OrderRepository(WallShopContext ctx, IServiceScopeFactory scopeFactory)
{
    public async Task<List<OrderDashboardDto>> GetAllOrdersForDashboardAsync(string LanguageCode = "en")
    {
       
        return await ctx.Orders
            .Include(o => o.OrderDetailsList)
             .Include(o => o.Customers) 
            .OrderByDescending(o => o.OrderDate) 
            .Select(o => new OrderDashboardDto
            {
                Id = o.Id,
              
                CustomerName = o.ReceiverName ?? "Guest",
                CustomerEmail = o.Customers.Email , 
                PhoneNumber = o.ReceiverPhone,
                Address = o.ShippingAddress,

                OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                ItemsCount = o.OrderDetailsList.Count,

                TotalPrice = o.TotalAmount,
                Status = LanguageCode == "ar" ? (o.Status == "Confirmed" ? "مؤكد" : o.Status == "Pending" ? "معلق" : "ملغي") : o.Status,
                Note=o.Note,
            })
            .ToListAsync();
    }
    public async Task<bool> ConfirmOrderStatusAsync(int orderId)
    {
        // نجلب الطلب من قاعدة البيانات
        var order = await ctx.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return false;

        // تحديث حالة الطلب
        order.Status = "Confirmed";
        await ctx.SaveChangesAsync();

        // إطلاق مهمة إرسال الإيميل في الخلفية دون تعطيل الـ Response
        // نمرر الـ UserId لنجلب الإيميل الخاص به
        NotifyCustomerInBackground(order.Id, order.UserId, order.ReceiverName, order.TotalAmount);

        return true;
    }

    // دالة إرسال الإيميل للعميل في الخلفية
    private void NotifyCustomerInBackground(int orderId, string? userId, string? customerName, decimal totalAmount)
    {
        // التأكد من أن المستخدم مسجل ولديه ID
        if (string.IsNullOrEmpty(userId)) return;

        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();

            var scopedContext = scope.ServiceProvider.GetRequiredService<WallShopContext>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                // 1. جلب إيميل العميل من جدول الـ Users
                var userEmail = await scopedContext.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(userEmail)) return;

                // 2. تجهيز تصميم الإيميل (HTML Template)
                string subject = $"✅ تم تأكيد طلبك رقم #{orderId} - Walls Shop";

                string commonStyle = @"
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f6; margin: 0; padding: 0; }
                    .container { max-width: 600px; margin: 30px auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 5px 20px rgba(0,0,0,0.05); overflow: hidden; }
                    .header { background-color: #27ae60; padding: 25px; text-align: center; }
                    .header h1 { color: #ffffff; margin: 0; font-size: 26px; letter-spacing: 1px; }
                    .content { padding: 40px 30px; text-align: right; direction: rtl; }
                    .greeting { font-size: 22px; color: #2c3e50; font-weight: bold; margin-bottom: 15px; }
                    .message { color: #555555; font-size: 16px; line-height: 1.8; margin-bottom: 25px; }
                    .order-details { background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 8px; padding: 20px; margin-bottom: 25px; }
                    .order-details p { margin: 10px 0; color: #333; font-size: 16px; }
                    .highlight { color: #27ae60; font-weight: bold; font-size: 18px; }
                    .btn-wrap { text-align: center; margin-top: 30px; }
                    .btn { background-color: #27ae60; color: #ffffff !important; padding: 14px 35px; text-decoration: none; border-radius: 50px; font-weight: bold; font-size: 16px; display: inline-block; box-shadow: 0 4px 10px rgba(39, 174, 96, 0.3); }
                    .footer { background-color: #ecf0f1; padding: 20px; text-align: center; font-size: 13px; color: #7f8c8d; }
                ";

                string htmlMessage = $@"
                <!DOCTYPE html>
                <html lang='ar'>
                <head>
                    <style>
                        @import url('https://fonts.googleapis.com/css2?family=Cairo:wght@400;600;700&display=swap');
                        {commonStyle}
                        body, .container {{ font-family: 'Cairo', Tahoma, sans-serif; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Walls Shop</h1>
                        </div>
                        <div class='content'>
                            <div class='greeting'>مرحباً {customerName ?? "عميلنا العزيز"}،</div>
                            <div class='message'>
                                يسعدنا إخبارك بأنه قد تم تأكيد طلبك بنجاح! جاري الآن تجهيز الأثاث الخاص بك لشحنه في أقرب وقت ممكن.
                            </div>
                            
                            <div class='order-details'>
                                <p>🛒 <strong>رقم الطلب:</strong> #{orderId}</p>
                                <p>💰 <strong>الإجمالي:</strong> <span class='highlight'>{totalAmount:N2} ج.م</span></p>
                                <p>📦 <strong>حالة الطلب:</strong> <span style='color:#27ae60; font-weight:bold;'>تم التأكيد</span></p>
                            </div>

                            <div class='btn-wrap'>
    <a href='https://wa.me/201027016323?text=مرحباً، أود الاستفسار عن طلبي رقم %23{orderId} من Walls Shop' 
       class='btn' 
       style='background-color: #25D366; box-shadow: 0 4px 10px rgba(37, 211, 102, 0.3);'>
       💬 تواصل معنا عبر واتساب
    </a>
</div>
                        </div>
                        <div class='footer'>
                            <p>شكراً لتسوقك من Walls Shop للأثاث المودرن.</p>
                            <p>© {DateTime.Now.Year} جميع الحقوق محفوظة.</p>
                        </div>
                    </div>
                </body>
                </html>";

                // 3. إرسال الإيميل للعميل
                await scopedEmailService.SendEmailAsync(userEmail, subject, htmlMessage);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ بصمت لكي لا يتعطل النظام
                Console.WriteLine($"Error sending order confirmation email to customer: {ex.Message}");
            }
        });
    }
    //public async Task<bool> ConfirmOrderStatusAsync(int orderId)
    //{
    //    var order = await ctx.Orders.FindAsync(orderId);
    //    if (order == null) return false;

    //    order.Status = "Confirmed";
    //    await ctx.SaveChangesAsync();
    //    return true;
    //}
    public async Task<bool> CancelOrderStatusAsync(int orderId)
    {
        var order = await ctx.Orders.FindAsync(orderId);
        if (order == null) return false;

        order.Status = "Canceled";
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var order = await ctx.Orders.FindAsync(orderId);
        if (order == null) return false;

        ctx.Orders.Remove(order);
        await ctx.SaveChangesAsync();
        return true;
    }
    //public async Task<OrderDetailsDto?> GetOrderDetailsByIdAsync(int orderId,string LanguageCode="en")
    //{
    //    var order = await ctx.Orders
    //        .Include(o => o.OrderDetailsList).ThenInclude(od => od.Variant)
    //        .Include(o => o.OrderDetailsList).ThenInclude(od => od.Product).ThenInclude(od=>od.NewProductColors).ThenInclude(od=>od.Color)
    //        .Include(o => o.OrderDetailsList).ThenInclude(od => od.Product).ThenInclude(od=>od.Images)
    //        .Include(o => o.Customers) 
    //        .FirstOrDefaultAsync(o => o.Id == orderId);

    //   // var colors= await ctx.Colors.ToListAsync();
    //    if (order == null) return null;
    //    if (LanguageCode == "ar")
    //    {
    //        return new OrderDetailsDto
    //        {
    //            Id = order.Id,
    //            CustomerName = order.ReceiverName ?? "Guest",
    //            CustomerEmail = order.Customers.Email ?? "N/A",
    //            PhoneNumber = order.ReceiverPhone,
    //            Address = order.ShippingAddress,
    //            OrderDate = order.OrderDate.ToString("MMM dd, yyyy"),
    //            TotalPrice = order.TotalAmount,
    //            Status = order.Status,

    //            Items = order.OrderDetailsList.Select(od => new OrderItemDto
    //            {
    //                ProductId = od.ProductId,
    //                ProductName = od.ProductName,
    //                //VariantId = od.VariantId,
    //                // Type = od.Type,
    //                // Size= od.Size,
    //                ImageUrl=od.Product.Images.Select(i=>i.RelativePath).ToList(),
    //                Type = od.Variant.Type,
    //                Size = od.Variant.Size,
    //                Color = od.Product.NewProductColors.FirstOrDefault()?.Color.ColorName ?? od.Color,
    //                Price = od.Price,
    //                Quantity = od.Quantity
    //            }).ToList()
    //        };
    //    }
    //    else
    //    {
    //        return new OrderDetailsDto
    //        {
    //            Id = order.Id,
    //            CustomerName = order.ReceiverName ?? "Guest",
    //            CustomerEmail = order.Customers.Email ?? "N/A",
    //            PhoneNumber = order.ReceiverPhone,
    //            Address = order.ShippingAddress,
    //            OrderDate = order.OrderDate.ToString("MMM dd, yyyy"),
    //            TotalPrice = order.TotalAmount,
    //            Status = order.Status,

    //            Items = order.OrderDetailsList.Select(od => new OrderItemDto
    //            {
    //                ProductId = od.ProductId,
    //                ProductName =od.Product.NameEN, //od.ProductName,
    //                                                //VariantId = od.VariantId,
    //                                                // Type = od.Type,
    //                                                // Size= od.Size,
    //                ImageUrl = od.Product.Images.Select(i => i.RelativePath).ToList(),

    //                Type = od.Variant.EnglishType,
    //                Size = od.Variant.EnglishSize,
    //                Color = od.Product.NewProductColors.FirstOrDefault()?.Color.EnglishColor ?? od.Color,

    //                Price = od.Price,
    //                Quantity = od.Quantity
    //            }).ToList()
    //        };
    //    }

    //    }

    public async Task<OrderDetailsDto?> GetOrderDetailsByIdAsync(int orderId, string LanguageCode = "en")
    {
        // أضفنا AsSplitQuery لتحسين الأداء ومنع مشكلة Cartesian Explosion الناتجة عن كثرة الـ Includes
        var order = await ctx.Orders
            .Include(o => o.OrderDetailsList).ThenInclude(od => od.Variant)
            .Include(o => o.OrderDetailsList).ThenInclude(od => od.Product).ThenInclude(p => p.NewProductColors).ThenInclude(npc => npc.Color)
            .Include(o => o.OrderDetailsList).ThenInclude(od => od.Product).ThenInclude(p => p.Images)
            .Include(o => o.Customers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        bool isArabic = LanguageCode.ToLower() == "ar";

        return new OrderDetailsDto
        {
            Id = order.Id,
            CustomerName = order.ReceiverName ?? "Guest",
            // حماية من الـ Null لو لم يكن هناك Customer
            CustomerEmail = order.Customers?.Email ,
            PhoneNumber = order.ReceiverPhone,
            Address = order.ShippingAddress,
            OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
            TotalPrice = order.TotalAmount,
            //    Status = order.Status,
            Status = LanguageCode == "ar" ? (order.Status == "Confirmed" ? "مؤكد" : order.Status == "Pending" ? "معلق" : "ملغي") : order.Status,

            Note = order.Note,
            // حماية الـ List والتأكد من عدم حدوث NullReference
            Items = order.OrderDetailsList?.Select(od => new OrderItemDto
            {
                ProductId = od.ProductId,

                // تحديد الاسم بناءً على اللغة
                ProductName = isArabic ? od.ProductName : (od.Product?.NameEN ?? od.ProductName),

                // حماية الـ Images من الـ Null
                ImageUrl = od.Product?.Images?.Select(i => i.RelativePath).ToList() ?? new List<string>(),

                // حماية الـ Variant من الـ Null
                Type = isArabic ? (od.Variant?.Type) : (od.Variant?.EnglishType ),
                Size = isArabic ? (od.Variant?.Size ) : (od.Variant?.EnglishSize ),

                // حماية الألوان المدمجة
                Color = isArabic
                    ? (od.Product?.NewProductColors?.FirstOrDefault()?.Color?.ColorName ?? od.Color )
                    : (od.Product?.NewProductColors?.FirstOrDefault()?.Color?.EnglishColor ?? od.Color ),

                Price = od.Price,
                Quantity = od.Quantity
            }).ToList() ?? new List<OrderItemDto>()
        };
    }
}