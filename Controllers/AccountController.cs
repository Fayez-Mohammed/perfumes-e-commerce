using Azure.Core;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.DTO.OTP_Dtos;
using WallsShop.Entity;
using WallsShop.Repository;
using WallsShop.Services;
using ResetPasswordRequest = WallsShop.DTO.ResetPasswordRequest;
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly WallShopContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly CartService _cartService;
    private readonly WishlistRepository _wishlistService;
    private readonly ILogger<AccountController> _logger;

	public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ITokenService tokenService, WallShopContext context
        , IPasswordHasher<User> passwordHasher, IEmailService emailService
        , CartService cartService, WishlistRepository wishlistService
        , ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _cartService = cartService;
        _wishlistService = wishlistService;
        _logger = logger;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model, [FromQuery] string LanguageCode = "en")
    {
        var user = new User { UserName = model.Email, Email = model.Email ,Name = model.DisplayName, PhoneNumber = model.PhoneNumber};
       
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null) return BadRequest(new {response = LanguageCode == "ar" ? "البريد الإلكتروني مستخدم بالفعل." : "Email is already in use."});

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded) return BadRequest(new {response = result.Errors});

        
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

       
        var frontendUrl = "https://e-commerce-six-olive-70.vercel.app";
        var callbackUrl = $"{frontendUrl}?token={encodedToken}&email={user.Email}";

        //await _emailService.SendEmailAsync(user.Email, "Confirm your email",
        //    $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.");
      
var htmlMessage = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirm Your WallsShop Account</title>
</head>

<body style='margin:0; padding:0; background-color:#f5f3ef; font-family:Arial, Helvetica, sans-serif; color:#2c2926;'>

    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f5f3ef; padding:40px 15px;'>
        <tr>
            <td align='center'>

                <table width='600' cellpadding='0' cellspacing='0' border='0'
                       style='max-width:600px; width:100%; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 30px rgba(0,0,0,0.08);'>

                    <!-- Header -->
                    <tr>
                        <td align='center' style='background:#1f1d1a; padding:32px 20px;'>
                            <div style='font-size:30px; font-weight:bold; letter-spacing:3px; color:#ffffff;'>
                                WALLSSHOP
                            </div>

                            <div style='font-size:12px; letter-spacing:4px; color:#c8a878; margin-top:7px;'>
                                FURNITURE
                            </div>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style='padding:45px 40px 35px;'>

                            <h1 style='margin:0 0 18px; text-align:center; font-size:27px; color:#292621;'>
                                Welcome to WallsShop
                            </h1>

                            <p style='margin:0 0 15px; text-align:center; font-size:16px; line-height:1.7; color:#66615b;'>
                                Thank you for creating your account with us.
                            </p>

                            <p style='margin:0 0 30px; text-align:center; font-size:15px; line-height:1.7; color:#77716a;'>
                                Please confirm your email address to activate your account
                                and start exploring our premium furniture collection.
                            </p>

                            <!-- Button -->
                            <table cellpadding='0' cellspacing='0' border='0' align='center'>
                                <tr>
                                    <td align='center' style='border-radius:8px; background:#b08a5a;'>
                                        <a href='{callbackUrl}'
                                           style='display:inline-block; padding:15px 38px; font-size:15px; font-weight:bold; color:#ffffff; text-decoration:none; border-radius:8px;'>
                                            Confirm My Email
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <p style='margin:32px 0 8px; text-align:center; font-size:13px; color:#99938b;'>
                                Or copy and paste this link into your browser:
                            </p>

                            <p style='margin:0; text-align:center; font-size:12px; line-height:1.6; word-break:break-all;'>
                                <a href='{callbackUrl}' style='color:#a47d4d; text-decoration:none;'>
                                    {callbackUrl}
                                </a>
                            </p>

                            <div style='height:1px; background:#eeeae4; margin:32px 0;'></div>

                            <p style='margin:0; text-align:center; font-size:12px; line-height:1.6; color:#99938b;'>
                                If you did not create a WallsShop account, you can safely ignore this email.
                            </p>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align='center' style='background:#f8f6f2; padding:24px 20px;'>

                            <div style='font-size:13px; font-weight:bold; letter-spacing:2px; color:#3b3732;'>
                                WALLSSHOP FURNITURE
                            </div>

                            <p style='margin:8px 0 0; font-size:11px; color:#99938b;'>
                                Premium furniture • Modern living • Crafted with quality
                            </p>

                            <p style='margin:12px 0 0; font-size:11px; color:#aaa49d;'>
                                © 2026 WallsShop Furniture. All rights reserved.
                            </p>

                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>";

        await _emailService.SendEmailAsync(
            user.Email,
            "Confirm your WallsShop account",
            htmlMessage);


      //  var maxUserNumber = await _context.Users.MaxAsync(u => (int?)u.UserNumber) ?? 0;
    //    user.UserNumber = maxUserNumber + 1;
        //await _context.SaveChangesAsync();
        return Ok(new {response = LanguageCode == "ar" ? "تم التسجيل بنجاح. يرجى التحقق من بريدك الإلكتروني لتأكيد حسابك." : "Registration successful. Please check your email to verify your account.",token=encodedToken});
    }
    
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto model, [FromQuery] string LanguageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(model.Email))
            return BadRequest(new {response = LanguageCode == "ar" ? "معلمات غير صالحة." : "Invalid parameters."});

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return NotFound(new {response = LanguageCode == "ar" ? "المستخدم غير موجود." : "User not found."});

        try 
        {
            string processedToken = model.Token.Replace(" ", "+");
            byte[] decodedBytes = WebEncoders.Base64UrlDecode(processedToken);
            string actualToken = Encoding.UTF8.GetString(decodedBytes);

            
            var result = await _userManager.ConfirmEmailAsync(user, actualToken);

            await _userManager.AddToRoleAsync(user,"User");

            if (result.Succeeded)
            { 
                return Ok(new {response = LanguageCode == "ar" ? "تم التحقق من البريد الإلكتروني!" : "Email verified!"});
            }

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return BadRequest(new {response = LanguageCode == "ar" ? "تنسيق الرمز غير صالح. تأكد من عدم تعديل الرابط." : "The token format is invalid. Ensure the link wasn't modified."});
        }
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto, [FromQuery] string LanguageCode="en")
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null) return Unauthorized(new {response = LanguageCode == "ar" ? "البريد الإلكتروني أو كلمة المرور غير صحيحة" : "Email Or Password Is Invalid"});
            if (user.IsBlocked)
            {
                return Unauthorized(new { response = LanguageCode == "ar" ? "تم حظر حسابك. يرجى الاتصال بالدعم للحصول على المساعدة." : "Your account has been blocked. Please contact support for assistance." });
            }
            var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordValid) return Unauthorized( new {response = LanguageCode == "ar" ? "البريد الإلكتروني أو كلمة المرور غير صحيحة" : "Email Or Password Is Invalid"});

            if (loginDto.Item?.Items != null)
            {
                foreach (var item in loginDto.Item.Items)
                    await _cartService.AddToCart(user.Id, item);
            }
 
            if (loginDto.Wishlist?.ProductIds?.Any() == true)
                await _wishlistService.AddToWishlist(loginDto.Wishlist, user.Id);
         if(user.EmailConfirmed == false)
            {
                return Unauthorized(new { response = LanguageCode == "ar" ? "البريد الإلكتروني غير مؤكد. يرجى التحقق من بريدك الإلكتروني قبل تسجيل الدخول." : "Email not confirmed. Please verify your email before logging in." });
            }
            var token = _tokenService.CreateToken(user);

            return Ok(new { Email = user.Email, Token = token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LanguageCode == "ar" ? "حدث خطأ أثناء تسجيل الدخول للبريد الإلكتروني: {Email}" : "Error occurred during login for email: {Email}", loginDto.Email);
			 
			try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "login_error.txt");
                System.IO.File.WriteAllText(path, ex.ToString());
            }
            catch {   }

            throw;
        }
    }


        
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { response  = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("user-info")]
    public async Task<IActionResult> GetCurrentUser()
    {
        
        var email = User.FindFirstValue(ClaimTypes.Email); 
       
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return BadRequest();
        
        var Roles = await _userManager.GetRolesAsync(user);


        // var role = Roles.FirstOrDefault();
        var role = Roles.ToList();
        if (email == null) return Unauthorized();

        //var user = await _userManager.FindByEmailAsync(email);
    
        return Ok(new {
            user.Name,
            user.Email,
            user.PhoneNumber,
            role 
        });
    }
    
    [Authorize]
    [HttpPut("update-user-info")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserDto updateUserDto, [FromQuery] string LanguageCode = "en")
    {
        var user = await _userManager.FindByEmailAsync(updateUserDto.Email);
        if (user == null) return NotFound(new {response = LanguageCode == "ar" ? "المستخدم غير موجود" : "User not found"});

        user.Name = updateUserDto.Name ?? user.Name;
        user.PhoneNumber = updateUserDto.PhoneNumber ?? user.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(new {response = result.Errors});

        return Ok(new { response  = LanguageCode == "ar" ? "تم تحديث معلومات المستخدم بنجاح" : "User information updated successfully" });
    }   
 
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model, [FromQuery] string LanguageCode = "en")
    {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _context.Users.FindAsync(userId);

        if (user == null) return Unauthorized();

       
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return BadRequest(new {response = LanguageCode == "ar" ? "كلمة المرور القديمة التي أدخلتها غير صحيحة." : "The old password you entered is incorrect."});
        }

        
        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new {response = LanguageCode == "ar" ? "تم تحديث كلمة المرور بنجاح." : "Password updated successfully."});
    }
    
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordDto model, [FromQuery] string LanguageCode = "en")
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return Ok(new {response  = LanguageCode == "ar" ? "إذا كان بريدك الإلكتروني موجودًا، فقد تم إرسال OTP." : "If your email exists, an OTP has been sent."});

       
        var otp = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

        await _emailService.SendEmailAsync(user.Email, "Reset Password OTP",
            $"Your password reset code is: {otp}. It expires in 15 minutes.");

        return Ok(new { response  = LanguageCode == "ar" ? "تم إرسال OTP إلى البريد الإلكتروني." : "OTP sent to email.", Email = model.Email });
    }
    
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest model, [FromQuery] string LanguageCode = "en")
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return BadRequest(new {response = LanguageCode == "ar" ? "طلب غير صالح." : "Invalid request."});

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", model.Otp);

        if (!isValid) return BadRequest(new { response = LanguageCode == "ar" ? "OTP غير صالح أو منتهي الصلاحية." : "Invalid or expired OTP."});

         
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        return Ok(new {
            Message = LanguageCode == "ar" ? "تم التحقق من OTP." : "OTP Verified.",
            ResetToken = resetToken
        });
    }
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest model, [FromQuery] string LanguageCode = "en")
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return Ok(new {response = LanguageCode == "ar" ? "تم إعادة إرسال OTP إذا كان الحساب موجودًا." : "OTP resent if account exists."});

        var otp = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
    
        await _emailService.SendEmailAsync(user.Email, "New OTP Code", 
            $"Your new code is: {otp}");

        return Ok(new {response  = LanguageCode == "ar" ? "تم إرسال OTP جديد." : "A new OTP has been sent."});
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model, [FromQuery] string LanguageCode = "en")
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return BadRequest(new {response = LanguageCode == "ar" ? "طلب غير صالح." : "Invalid Request"});

         
        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

        if (result.Succeeded)
        {
            return Ok(new {response = LanguageCode == "ar" ? "تم إعادة تعيين كلمة المرور بنجاح." : "Your password has been reset successfully."});
        }

        return BadRequest(result.Errors);
    }
  
}