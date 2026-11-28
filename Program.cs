using Microsoft.AspNetCore.Authentication.JwtBearer; // Add this
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
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

// Load .env (if present) before the builder reads configuration.
// Real environment variables take precedence over values in the file.
LoadDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

static void LoadDotEnv(string path)
{
    if (!File.Exists(path)) return;

    foreach (var rawLine in File.ReadLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;

        var separator = line.IndexOf('=');
        if (separator <= 0) continue;

        var name = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim();

        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        if (name.Length == 0 || Environment.GetEnvironmentVariable(name) is not null)
            continue;

        Environment.SetEnvironmentVariable(name, value);
    }
}

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
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // If token was sent directly without 'Bearer ' prefix, accept it seamlessly
                context.Token = authHeader.Trim();
            }
            return Task.CompletedTask;
        },
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
builder.Services.AddHostedService<CartCleanupBackgroundService>();
// 1. Load settings from appsettings.json
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<PaymobSettings>(builder.Configuration.GetSection("Paymob"));
builder.Services.AddHttpClient<IPaymobService, PaymobService>();
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
builder.Services.AddScoped<DashboardVideoRepository>();
builder.Services.AddScoped<VideoRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600; // 100 MB for video uploads
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<ProductViewTracker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
////////////////
//builder.Services.AddDistributedMemoryCache();

//builder.Services.AddScoped<CartService>();
////////////////////
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });

});
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAngular",
//        policy =>
//        {
//            policy.WithOrigins(
//                "http://localhost:4200",             // Local Development
//                "http://localhost:3000",             // Local Development
//                "https://mariam-perfumes-next-latest.vercel.app", // Production frontend
//                "https://e-commerce-six-olive-70.vercel.app", // Old frontend
//                "https://wallsshop-2.vercel.app",
//                "https://wallsshop-furniture.com",    // Netlify Deployment
//                "http://wallsshop-furniture.com",    // Netlify Deployment
//                "https://www.wallsshop-furniture.com",    // Netlify Deployment
//                "http://www.wallsshop-furniture.com"    // Netlify Deployment


//             )
//                  .AllowAnyHeader()
//                  .AllowAnyMethod()
//                  .AllowCredentials();

//        });
//});

//////////Swagger\\\\\\\\\\\\\\\\\\\\
///

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "WallsShop API", Version = "v1" });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Enter your JWT token directly. Swagger will automatically prefix it with 'Bearer '.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
                }
            },
            Array.Empty<string>()
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
var staticFileContentTypeProvider = new FileExtensionContentTypeProvider();
staticFileContentTypeProvider.Mappings[".mp4"] = "video/mp4";
staticFileContentTypeProvider.Mappings[".webm"] = "video/webm";
staticFileContentTypeProvider.Mappings[".mov"] = "video/quicktime";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileContentTypeProvider
});
app.UseRouting();
//app.UseCors("AllowAngular");
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseMiddleware<BlockedUserMiddleware>();

app.UseAuthorization(); 

app.MapControllers();
app.MapHub<ProductViewHub>("/hubs/product-views");
app.Run();