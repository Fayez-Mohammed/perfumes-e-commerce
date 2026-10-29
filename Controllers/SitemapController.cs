

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WallsShop.Context;

namespace WallsShop.Controllers;

[ApiController]
public class SitemapController(WallShopContext ctx) : ControllerBase
{
    // رابط الواجهة الأمامية (Angular)
    private readonly string _frontendUrl = "https://wallsshop-furniture.com";

    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> GenerateSitemap()
    {
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

        var basePages = new[]
        {
            "", "/shop", "/categories", "/faqs", "/contact"
        };

        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        foreach (var page in basePages)
        {
            var arUrl = page == "" ? $"{_frontendUrl}/" : $"{_frontendUrl}{page}";
            var enUrl = page == "" ? $"{_frontendUrl}/en" : $"{_frontendUrl}/en{page}";

            var priority = page switch
            {
                "" or "/home" => "1.0",
                "/faqs" => "0.5",
                "/contact" => "0.6",
                _ => "0.8" 
            };

            var changeFreq = page.Contains("shop") ? "daily" : "weekly";

            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{arUrl}</loc>");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\" />");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\" />");
            sb.AppendLine($"    <lastmod>{currentDate}</lastmod>");
            sb.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");

            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{enUrl}</loc>");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\" />");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\" />");
            sb.AppendLine($"    <lastmod>{currentDate}</lastmod>");
            sb.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }

        var products = await ctx.Products
            .Select(p => new { p.Id, p.LastUpdate })
            .AsNoTracking()
            .ToListAsync();

        foreach (var product in products)
        {
            if (product.Id >= 2000)
                break;
            var lastMod = product.LastUpdate?.ToString("yyyy-MM-dd") ?? currentDate;
            var arUrl = $"{_frontendUrl}/product/{product.Id}";
            var enUrl = $"{_frontendUrl}/en/product/{product.Id}";

            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{arUrl}</loc>");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\" />");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\" />");
            sb.AppendLine($"    <lastmod>{lastMod}</lastmod>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.9</priority>");
            sb.AppendLine("  </url>");

            // النسخة الإنجليزية للمنتج
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{enUrl}</loc>");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\" />");
            sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\" />");
            sb.AppendLine($"    <lastmod>{lastMod}</lastmod>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.9</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [Produces("text/plain")]
    public IActionResult GenerateRobotsTxt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");

        sb.AppendLine("Disallow: /dashboard/");
        sb.AppendLine("Disallow: /profile/");
        sb.AppendLine("Disallow: /cart/");
        sb.AppendLine("Disallow: /checkout/");

        sb.AppendLine("Disallow: /en/dashboard/");
        sb.AppendLine("Disallow: /en/profile/");
        sb.AppendLine("Disallow: /en/cart/");
        sb.AppendLine("Disallow: /en/checkout/");

        sb.AppendLine("Disallow: /api/");

        sb.AppendLine();
        sb.AppendLine($"Sitemap: {_frontendUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}