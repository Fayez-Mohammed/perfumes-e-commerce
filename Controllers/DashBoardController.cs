using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.DTO.Dashboard;
using WallsShop.Entity;
using WallsShop.Properties.Entity;
using WallsShop.Repository;
using WallsShop.Repository.Dashboard;
using WallsShop.Services;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
namespace WallsShop.Controllers.Dashboard;


[ApiController]
[Route("api/dashboard/[controller]")]
[Authorize(Roles = "Admin")]
public class DashBoardController : ControllerBase
{
    private readonly ILogger<DashBoardController> _logger;
    private readonly WallShopContext _context;
    private readonly ProductRepository producrrepo;
    private readonly CartService _cartService;
    private readonly WishlistRepository _wishlistService;
    private readonly UserManager<User> _userManager;
    private readonly DashboardCategoryRepository _categoryRepository;
    private readonly DashboardOfferRepository _offerRepository;
    private readonly OrderRepository _orderRepository;
    private readonly WishlistRepository _wishlistRepository;
    private readonly FormRepository _formRepository;
    private readonly ProductRepository _productRepo;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly SignInManager<User> _signInManager;
    private readonly OrderRepository _orderRepo;
    public DashBoardController(
        DashboardCategoryRepository Categoryrepository,
        DashboardOfferRepository offerRepository,
        ProductRepository products,
        WallShopContext context,
        CartService cartService,
        WishlistRepository wishlistService,
        UserManager<User> userManager,
        OrderRepository orderRepository,
        WishlistRepository wishlistRepository,
        FormRepository formRepository,
       ProductRepository productRepo,
       SignInManager<User> signInManager,
       OrderRepository orderRepo,

      IWebHostEnvironment env,
        IConfiguration config,

        ILogger<DashBoardController> logger)
    {
        _logger = logger;
        _context = context;
        producrrepo = products;
        _cartService = cartService;
        _wishlistService = wishlistService;
        _userManager = userManager;
        _categoryRepository = Categoryrepository;
        _offerRepository = offerRepository;
        _productRepo = productRepo;
        _env = env;
        _config = config;
        _signInManager = signInManager;
        _orderRepo = orderRepo;
    }
    public class DashboardSummaryDto
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalOffers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
    }
    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        try
        {
            var totalProducts = await producrrepo.GetTotalProductsCount();
            var totalCategories = await _categoryRepository.GetTotalCategoriesCount();
            var totalOffers = await _offerRepository.GetTotalOffersCount();
            var totalUsers = await _userManager.Users.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalSales = await _context.Orders.Where(o=>o.Status=="Confirmed").SumAsync(o => o.TotalAmount);
            var summary = new DashboardSummaryDto
            {
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalOffers = totalOffers,
                TotalUsers = totalUsers,
                TotalOrders = totalOrders,
                TotalSales = totalSales

            };
            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard summary");
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب ملخص لوحة التحكم" });
        }
    }
    class ProductsToMangeDto
    {
        public int TotalProducts { get; set; }

        public List<DashboardProductDto> Products { get; set; }
    }

    /// <summary>
    /// POST: api/product/add-product
    /// إضافة منتج - كل شيء JSON
    /// </summary>
    [HttpPost("add-product")]

    public async Task<IActionResult> AddProduct([FromForm] ProductAddRequest request )
    {

        if (!ModelState.IsValid)
            return BadRequest(new { response = "بيانات غير صحيحة", errors = ModelState });
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var productDto = JsonSerializer.Deserialize<ProductAddDto0>(request.Product, options);
        if (productDto == null)
            return BadRequest("Invalid Product Data");
        var result = await _productRepo.AddProduct(productDto,request.Images, _env, _config);

        if (result)
            return Ok(new { response = "تم إضافة المنتج بنجاح" });

        return BadRequest(new { response = "فشل في إضافة المنتج" });
    }

    [HttpDelete("delete-product")]
    //  [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> DeleteProduct([FromQuery] int id)
    {
        if (id <= 0)
            return BadRequest(new { response = "معرف المنتج غير صحيح" });

        var (success, message) = await _productRepo.DeleteProduct(id, _env);

        if (success)
            return Ok(new { response = message });

        return BadRequest(new { response = message });
    }
    [HttpGet("GetAllCustomers")]
    public async Task<IActionResult> GetAllCustomers([FromQuery] string LanguageCode = "en")
    {
        try
        {
            var users = await _userManager.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            if (users == null || users.Count == 0)
            {
                return NotFound(new { success = false, message = LanguageCode == "ar" ? "لا يوجد عملاء" : "No customers found" });
            }

            var customerDtos = users
                .Select((user, index) => new CustomerreturnDto
                {
                    RowNum = index + 1,
                    Id = user.Id,
                    CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd"),
                    UserName = user.Name,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Status=user.IsBlocked? LanguageCode == "ar" ? "محظور" : "Blocked" : LanguageCode == "ar" ? "نشط" : "Active"
                })
                .ToList();

            return Ok(new { success = true, data = customerDtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers");
            return StatusCode(500, new { success = false, message = LanguageCode == "ar" ? "حدث خطأ أثناء جلب العملاء" : "Error fetching customers" });
        }
    }

 
    [HttpDelete("DeleteCustomer")]
    public async Task<IActionResult> DeleteCustomer([FromQuery] string id)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest(new { response = "معرف العميل غير صحيح" });
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { response = "العميل غير موجود" });
        if (user.Email?.ToLower() == "fayez00mohammed@gmail.com")
        {
            return BadRequest(new
            {
                response = "This user cannot be Deleted."
            });
        }
        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
            return Ok(new { response = "تم حذف العميل بنجاح" });

        // Aggregate error messages if any
        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
        return BadRequest(new { response = errorMessage });
    }
 
    [HttpPut("update-product")]
    public async Task<IActionResult> UpdateProduct( [FromForm] ProductUpdateRequest request, [FromQuery] string LanguageCode = "en")
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var productDto = JsonSerializer.Deserialize<ProductUpdateDto>(request.Product, options);

        var result = await _productRepo.UpdateProduct(request.Id, productDto, request.NewImages, request.DeleteImageIds, _env, _config);

        if (result)
            return Ok(new { response = LanguageCode == "ar" ? "تم تعديل المنتج بنجاح" : "Product updated successfully" });

        return BadRequest(new { response = LanguageCode == "ar" ? "فشل في تعديل المنتج" : "Failed to update product" });
    }
   
    

    [Authorize(Roles = "Admin")]
    [HttpPut("BlockUser")]
    public async Task<IActionResult> BlockUser([FromQuery] string userId)
    {
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { response = "User not found." });
        if (user.Email?.ToLower() == "fayez00mohammed@gmail.com")
        {
            return BadRequest(new
            {
                response = "This user cannot be blocked."
            });
        }
        user.IsBlocked = true;
        await _userManager.UpdateAsync(user);
        return Ok(new { response = "User has been blocked." });
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("UnBlockUser")]
    public async Task<IActionResult> UnBlockUser([FromQuery] string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { response = "User not found." });
        user.IsBlocked = false;
        await _userManager.UpdateAsync(user);
        return Ok(new { response = "User has been unblocked." });
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("ToggleBlock")]
    public async Task<IActionResult> ToggleBlock([FromQuery] string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { response = "User not found." });
        if (user.Email?.ToLower() == "fayez00mohammed@gmail.com")
        {
            return BadRequest(new
            {
                response = "This user cannot be Deleted."
            });
        }
        user.IsBlocked = !user.IsBlocked;
        await _userManager.UpdateAsync(user);
        if (user.IsBlocked)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Ok(new { response = user.IsBlocked ? "User has been blocked." : "User has been unblocked." });
    }

    [HttpGet("GetProductForEdit")]
    public async Task<IActionResult> GetProductForEdit([FromQuery] QueryParameters queryParameters)
    {
        string? userId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
        if (queryParameters.id <= 0)
            return BadRequest(new { response = "معرف المنتج غير صحيح" });
        var product = await _productRepo.GetArabicAndEnglishProductById(queryParameters, userId);
        if (product == null)
            return NotFound(new { response = "المنتج غير موجود" });
        return Ok(new { success = true, data = product });
    }
    [HttpPost("AddAdmin")]
    public async Task<IActionResult> AddAdmin([FromBody] AdminAddDto model, [FromQuery] string LanguageCode = "en")
    {
        if (!ModelState.IsValid)
            return BadRequest(new { response = LanguageCode == "ar" ? "بيانات غير صحيحة" : "Invalid data", errors = ModelState });

        var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value
                               ?? User.FindFirst("email")?.Value;

        if (currentUserEmail != "fayez00mohammed@gmail.com")
        {
            return Unauthorized(new
            {
                response = LanguageCode == "ar"
                    ? "غير مصرح لك بتنفيذ هذا الإجراء"
                    : "You are not authorized"
            });
        }



        var user = new User
        {
           
            UserName = model.Email,
            Email = model.Email,
            Name = model.Name,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow
            
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        user.EmailConfirmed = true;
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            var userDto = new adminreturnDto
            {
                Id = user.Id,
                UserName = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd"),
                Status = user.IsBlocked ? (LanguageCode == "ar" ? "محظور" : "Blocked") : (LanguageCode == "ar" ? "نشط" : "Active")
            };
            return Ok(new { response = userDto });
        }
        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
        return BadRequest(new { response = errorMessage });
    }
  

// ... داخل الـ Controller الخاص بك ...

[HttpGet("DownloadOrdersXml")]
public async Task<IActionResult> DownloadOrdersXml([FromQuery] string LanguageCode = "en")
{
    // 1. جلب بيانات الطلبات من الداتا بيز بنفس الدالة الممتازة الخاصة بك
    var orders = await _orderRepo.GetAllOrdersForDashboardAsync(LanguageCode);

    if (orders == null || !orders.Any())
    {
        return NotFound(new { Message = "No orders found to download." });
    }

    // 2. تجهيز محول الـ XML
    var serializer = new XmlSerializer(typeof(List<OrderDashboardDto>));

    // 3. تحويل قائمة الطلبات إلى نص XML
    using var stringWriter = new StringWriter();
    serializer.Serialize(stringWriter, orders);
    string xmlContent = stringWriter.ToString();

    // 4. تحويل النص إلى Bytes لكي يتم تحميله كملف
    byte[] fileBytes = Encoding.UTF8.GetBytes(xmlContent);

    // 5. إرجاع الملف للعميل باسم ديناميكي يحتوي على تاريخ اليوم
    string fileName = $"Orders_Export_{DateTime.Now:yyyyMMdd}.xml";

    return File(fileBytes, "application/xml", fileName);
}
//===================
[HttpGet("GetAllOrders")]
    public async Task<IActionResult> GetAllOrders([FromQuery] string LanguageCode = "en")
    {
        var orders = await _orderRepo.GetAllOrdersForDashboardAsync(LanguageCode);
        return Ok(orders);
    }

    [HttpPut("ConfirmOrder")]
    public async Task<IActionResult> ConfirmStatus([FromQuery]int id)
    {
        var result = await _orderRepo.ConfirmOrderStatusAsync(id);

        if (!result)
            return NotFound("Order not found");

        return Ok(new { message = "Order Confirmed successfully" });
    }
    [HttpPut("CancelOrder")]
    public async Task<IActionResult> CancelStatus([FromQuery]int id)
    {
        var result = await _orderRepo.CancelOrderStatusAsync(id);

        if (!result)
            return NotFound("Order not found");

        return Ok(new { message = "Order Canceld successfully" });
    }

    [HttpDelete("Delete-Order")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var result = await _orderRepo.DeleteOrderAsync(id);

        if (!result)
            return NotFound("Order not found");

        return Ok(new { message = "Order deleted successfully" });
    }
    [HttpGet("GetOrderDetails")]
    public async Task<IActionResult> GetOrderDetails(int id,string LanguageCode="en")
    {
        var orderDetails = await _orderRepo.GetOrderDetailsByIdAsync(id,LanguageCode);
        if (orderDetails == null)
            return NotFound(LanguageCode == "ar" ? "الطلب غير موجود" : "Order not found");

        return Ok(orderDetails);
    }
    [HttpGet("productsForDashboard")]
    public async Task<IActionResult> GetProducts
     ([FromQuery] QueryParameters queryParameters)
    {
        string? userId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
        if (queryParameters.LanguageCode == "ar" || string.IsNullOrEmpty(queryParameters.LanguageCode))
        {
            var list = await producrrepo.GetProductsOverviewForDashboard(queryParameters, userId);

            return Ok(new { Response = list });
        }
        var list1 = await producrrepo.GetProductsTranslationOverviewForDashboard(queryParameters, userId);

        return Ok(new { Response = list1 });
    }


}
public class AdminAddDto
{
    public required string Email { get;  set; }
    public required string Name { get;  set; }
    public required string PhoneNumber { get;  set; }
    public required string Password { get;  set; }
}