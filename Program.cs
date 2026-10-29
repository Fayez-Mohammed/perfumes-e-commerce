using Microsoft.AspNetCore.Authentication.JwtBearer; // Add this
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // Add this
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using WallsShop.Context;
using WallsShop.Entity;
using WallsShop.Helpers;

//using WallsShop.Helpers;
using WallsShop.Repository;
using WallsShop.Repository.Dashboard;
using WallsShop.Services;
//using Swashbuckle.AspNetCore.SwaggerGen;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WallShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<WallShopContext>()
    .AddDefaultTokenProviders();

// --- ADD THIS SECTION: JWT Configuration ---
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenKey"]));

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = key,
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        ValidateLifetime = true,
//        ClockSkew = TimeSpan.Zero
//    };
//});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            // 1. محاولة قراءة اللغة من الرابط (Query String) لأنك تستخدمها في الـ Controller
            string languageCode = context.Request.Query["languageCode"].ToString().ToLower();

            // 2. كـ Best Practice: إذا لم تكن في الرابط، نحاول قراءتها من الـ Headers (Accept-Language)
            if (string.IsNullOrEmpty(languageCode))
            {
                languageCode = context.Request.Headers["Accept-Language"].ToString().ToLower();
            }

            // 3. تحديد اللغة (إذا كان النص يحتوي على 'ar' نعتبرها عربي، وإلا إنجليزي كافتراضي)
            bool isArabic = languageCode.Contains("ar");

            // 4. تجهيز الرسالة بناءً على اللغة
            var message = isArabic
                ? "يجب تسجيل الدخول اولا"
                : "Unauthorized. Token is missing or invalid.";

            // 5. إرسال الـ JSON بنسخة واحدة (رسالة واحدة فقط)
            var result = JsonSerializer.Serialize(new
            {
                status = 401,
                message = message
            });

            await context.Response.WriteAsync(result);
        }
    };
});
// --------------------------------------------

builder.Services.AddSingleton<CartService>();
// 1. Load settings from appsettings.json
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<OfferRepository>();
builder.Services.AddScoped<WishlistRepository>();
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<FormRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ReviewRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<DashboardCategoryRepository>();
builder.Services.AddScoped<DashboardOfferRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ProductViewTracker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
////////////////
//builder.Services.AddDistributedMemoryCache();

//builder.Services.AddScoped<CartService>();
////////////////////
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", policy =>
//    {
//        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
//    });

//});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:4200",             // Local Development
                "http://localhost:3000",             // Local Development
                "https://e-commerce-six-olive-70.vercel.app",             // Local Development
                "https://wallsshop-2.vercel.app",
                "https://wallsshop-furniture.com",    // Netlify Deployment
                "http://wallsshop-furniture.com",    // Netlify Deployment
                "https://www.wallsshop-furniture.com",    // Netlify Deployment
                "http://www.wallsshop-furniture.com"    // Netlify Deployment


             )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();

        });
});

//////////Swagger\\\\\\\\\\\\\\\\\\\\
///

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "WallsShop API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

//////////////////////////
var app = builder.Build();

// --- 2. Middleware Pipeline ---
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WallsShop API v1");
    options.RoutePrefix = string.Empty;
}
);
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();

    //    app.UseSwagger();
    //    app.UseSwaggerUI();
    //}
//else
//{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
//}

//app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseRouting();
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseMiddleware<BlockedUserMiddleware>();

app.UseAuthorization(); 

app.MapControllers();
app.MapHub<ProductViewHub>("/hubs/product-views");
app.Run();