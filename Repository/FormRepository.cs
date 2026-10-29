using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.Entity;
using WallsShop.Services;

namespace WallsShop.Repository;

// استخدام C# 12 Primary Constructor للـ DI
public class FormRepository(WallShopContext ctx, IServiceScopeFactory scopeFactory)
{
    public async Task<bool> CreateForm(FormDto form)
    {
        var formData = new Form
        {
            UserAddress = string.IsNullOrWhiteSpace(form.Address) ? "غير محدد" : form.Address,
            PhoneNumber = form.PhoneNumber,
            Message = form.Message,
            Date = DateOnly.FromDateTime(DateTime.Now)
        };

        try
        {
            // استخدام ctx مباشرة
            ctx.Forms.Add(formData);
            await ctx.SaveChangesAsync();

            // إطلاق مهمة الإيميل في الخلفية
            NotifyAdminsInBackground(form);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void NotifyAdminsInBackground(FormDto form)
    {
        _ = Task.Run(async () =>
        {
            // استخدام scopeFactory مباشرة
            using var scope = scopeFactory.CreateScope();

            var scopedContext = scope.ServiceProvider.GetRequiredService<WallShopContext>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                // 1. جلب إيميلات المديرين
                var adminRoleId = await scopedContext.Roles
                    .Where(r => r.NormalizedName == "ADMIN")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (adminRoleId == null) return;

                var adminEmails = await scopedContext.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId)
                    .Join(scopedContext.Users, ur => ur.UserId, u => u.Id, (ur, u) => u.Email)
                    .Where(email => !string.IsNullOrEmpty(email))
                    .ToListAsync();

                if (!adminEmails.Any()) return;

                // 2. تصميم رسالة الـ HTML
                string subject = "🔔 رسالة تواصل جديدة - Walls Shop";

                string commonStyle = @"
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f6f6f6; margin: 0; padding: 0; }
                    .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); overflow: hidden; }
                    .header { background-color: #34495e; padding: 20px; text-align: center; }
                    .header h1 { color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px; }
                    .content { padding: 30px; text-align: right; direction: rtl; }
                    .title { color: #2980b9; font-size: 20px; font-weight: bold; margin-bottom: 20px; border-bottom: 2px solid #ecf0f1; padding-bottom: 10px; }
                    .info-box { background-color: #ecf0f1; padding: 15px; border-radius: 8px; margin-bottom: 20px; border-right: 4px solid #2980b9; }
                    .info-text { color: #333; font-size: 15px; line-height: 1.8; margin: 5px 0; }
                    .msg-box { background-color: #fff9c4; padding: 15px; border-radius: 8px; border: 1px dashed #f1c40f; color: #555; line-height: 1.6; }
                    .footer { background-color: #eeeeee; padding: 15px; text-align: center; font-size: 12px; color: #999999; }
                ";

                string htmlMessage = $@"
                <!DOCTYPE html>
                <html lang='ar'>
                <head>
                    <style>
                        @import url('https://fonts.googleapis.com/css2?family=Cairo:wght@400;700&display=swap');
                        {commonStyle}
                        body, .container {{ font-family: 'Cairo', Tahoma, sans-serif; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>إشعار إداري - Walls Shop</h1>
                        </div>
                        <div class='content'>
                            <div class='title'>يوجد استفسار جديد من عميل</div>
                            
                            <div class='info-box'>
                                <p class='info-text'>👤 <strong>الاسم:</strong> {form.Name}</p>
                                <p class='info-text'>📞 <strong>رقم الهاتف:</strong> {form.PhoneNumber}</p>
                                <p class='info-text'>📍 <strong>العنوان:</strong> {form.Address ?? "غير محدد"}</p>
                                <p class='info-text'>📧 <strong>البريد الإلكتروني:</strong> {form.Email ?? "غير محدد"}</p>
                                <p class='info-text'>📅 <strong>التاريخ:</strong> {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}</p>
                            </div>

                            <strong>نص الرسالة:</strong>
                            <div class='msg-box'>
                                {form.Message.Replace("\n", "<br/>")}
                            </div>
                        </div>
                        <div class='footer'>
                            <p>هذه الرسالة تم توليدها تلقائياً من نظام Walls Shop.</p>
                        </div>
                    </div>
                </body>
                </html>";

                // 3. إرسال الإيميلات
                foreach (var email in adminEmails)
                {
                    try
                    {
                        await scopedEmailService.SendEmailAsync(email!, subject, htmlMessage);
                    }
                    catch
                    {
                        // Ignored
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending admin notification: {ex.Message}");
            }
        });
    }
}









//using WallsShop.Context;
//using WallsShop.DTO;
//using WallsShop.Entity;

//namespace WallsShop.Repository;

//public class FormRepository(WallShopContext ctx)
//{ 
//    public async Task<bool> CreateForm(FormDto form)
//    {
//        var formData = new Form
//        {
//            UserAddress =  string.IsNullOrWhiteSpace(form.Address) ? "No Address" : form.Address,
//            PhoneNumber = form.PhoneNumber,
//            Message =form.Message,
//            Date =  new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)
//        };

//        try
//        {
//            ctx.Forms.Add(formData);
//            await ctx.SaveChangesAsync();
//            return true;
//        }
//        catch (Exception e)
//        {
//            return false;
//        } 
//    }
//}