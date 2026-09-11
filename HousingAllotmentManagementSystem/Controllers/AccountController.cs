using System.Security.Claims;
using System.Security.Cryptography;
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using HousingAllotmentManagementSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =========================================================
        // LOGIN - GET
        // =========================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard");
                }

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            ViewBag.ReturnUrl = returnUrl;

            return View();
        }


        // =========================================================
        // LOGIN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string? loginId,
            string? password,
            bool rememberMe = false,
            string? returnUrl = null)
        {
            // -----------------------------------------------------
            // VALIDATE LOGIN ID
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(loginId))
            {
                ModelState.AddModelError(
                    "loginId",
                    "Please enter your email or phone number.");
            }

            // -----------------------------------------------------
            // VALIDATE PASSWORD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Please enter your password.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // Remove spaces
            loginId = loginId.Trim();

            // -----------------------------------------------------
            // NORMALIZE PHONE
            // -----------------------------------------------------

            string normalizedPhone =
                NormalizePhoneNumber(loginId);

            // -----------------------------------------------------
            // FIND USER
            // -----------------------------------------------------

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == loginId ||
                    u.Mobile == normalizedPhone);

            // -----------------------------------------------------
            // USER NOT FOUND
            // -----------------------------------------------------

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "No account was found with this email or phone number.");

                ViewBag.ReturnUrl = returnUrl;

                return View();
            }

            // -----------------------------------------------------
            // ACCOUNT STATUS
            // -----------------------------------------------------

            if (user.Status != true)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is inactive. Please contact the administrator.");

                ViewBag.ReturnUrl = returnUrl;

                return View();
            }

            // -----------------------------------------------------
            // PASSWORD CHECK
            // -----------------------------------------------------

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError(
                    "",
                    "No password is configured for this account.");

                ViewBag.ReturnUrl = returnUrl;

                return View();
            }

            if (user.PasswordHash != password)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid password.");

                ViewBag.ReturnUrl = returnUrl;

                return View();
            }

            // -----------------------------------------------------
            // VERIFIED CHECK
            // -----------------------------------------------------

            if (user.IsVerified != true)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has not been verified yet.");

                ViewBag.ReturnUrl = returnUrl;

                return View();
            }

            // =====================================================
            // GET ROLE
            // =====================================================

            string databaseRole =
                user.Role?.RoleName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(databaseRole))
            {
                ModelState.AddModelError(
                    "",
                    "No role is assigned to this account. Please contact the administrator.");

                ViewBag.ReturnUrl = returnUrl;

                return View();
            }

            // Only two roles are allowed
            string roleName;

            if (databaseRole.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                roleName = "Admin";
            }
            else if (databaseRole.Equals(
                    "Client",
                    StringComparison.OrdinalIgnoreCase))
            {
                roleName = "Client";
            }
            else
            {
                ModelState.AddModelError(
                    "",
                    "Invalid account role. Please contact the administrator.");

                ViewBag.ReturnUrl = returnUrl;

                return View();
            }

            // =====================================================
            // REMOVE EXISTING LOGIN COOKIE
            // =====================================================

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // =====================================================
            // CREATE CLAIMS
            // =====================================================

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName ?? string.Empty),

                new Claim(
                    ClaimTypes.Email,
                    user.Email ?? string.Empty),

                new Claim(
                    ClaimTypes.MobilePhone,
                    user.Mobile ?? string.Empty),

                new Claim(
                    ClaimTypes.Role,
                    roleName)
            };

            // =====================================================
            // CREATE IDENTITY
            // =====================================================

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // =====================================================
            // AUTHENTICATION PROPERTIES
            // =====================================================

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,

                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8),

                AllowRefresh = true
            };

            // =====================================================
            // SIGN IN
            // =====================================================

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            // =====================================================
            // ADMIN
            // =====================================================

            if (roleName.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            // =====================================================
            // CLIENT
            // =====================================================

            if (IsClientAllowedReturnUrl(returnUrl))
            {
                return Redirect(returnUrl!);
            }

            return RedirectToAction(
                "Index",
                "Home");
        }


        // =========================================================
        // REGISTER - GET
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // =========================================================
        // REGISTER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Normalize email
            string email = model.Email.Trim().ToLower();

            // Normalize phone number
            string mobile = NormalizePhoneNumber(model.Mobile);

            // =====================================================
            // CHECK EMAIL
            // =====================================================

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email address already exists.");

                return View(model);
            }

            // =====================================================
            // CHECK MOBILE
            // =====================================================

            bool mobileExists = await _context.Users
                .AnyAsync(u => u.Mobile == mobile);

            if (mobileExists)
            {
                ModelState.AddModelError(
                    "Mobile",
                    "An account with this phone number already exists.");

                return View(model);
            }

            // =====================================================
            // FIND CLIENT ROLE
            // =====================================================

            var clientRole = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.RoleName == "Client");

            if (clientRole == null)
            {
                ModelState.AddModelError(
                    "",
                    "Client role was not found in the database. Please contact the administrator.");

                return View(model);
            }

            // =====================================================
            // CREATE USER
            // =====================================================

            var user = new User
            {
                RoleId = clientRole.RoleId,

                FullName = model.FullName.Trim(),

                Email = email,

                Mobile = mobile,

                // Matches your current login system
                PasswordHash = model.Password,

                IsVerified = true,

                Status = true,

                CreatedDate = DateTime.Now
            };

            // =====================================================
            // SAVE USER
            // =====================================================

            try
            {
                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Registration successful! You can now login using your email or phone number.";

                return RedirectToAction(nameof(Login));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to create your account. Email or phone number may already exist.");

                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred while creating your account.");

                return View(model);
            }
        }


        // =========================================================
        // FORGOT PASSWORD - GET
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        // =========================================================
        // FORGOT PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == email);

            // -----------------------------------------------------
            // SECURITY
            // -----------------------------------------------------
            // We don't reveal whether an email exists.
            // -----------------------------------------------------

            if (user == null)
            {
                TempData["ForgotPasswordMessage"] =
                    "If an account exists with this email address, a password reset link has been sent.";

                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            // -----------------------------------------------------
            // GENERATE SECURE TOKEN
            // -----------------------------------------------------

            byte[] tokenBytes =
                RandomNumberGenerator.GetBytes(32);

            string token =
                Convert.ToBase64String(tokenBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

            // -----------------------------------------------------
            // SAVE TOKEN
            // -----------------------------------------------------

            user.PasswordResetToken = token;

            // Token valid for 30 minutes
            user.PasswordResetTokenExpiry =
                DateTime.UtcNow.AddMinutes(30);

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // CREATE RESET LINK
            // -----------------------------------------------------

            string? resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    email = user.Email,
                    token = token
                },
                Request.Scheme);

            // -----------------------------------------------------
            // EMAIL CONTENT
            // -----------------------------------------------------

            string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Password Reset</title>
</head>

<body style='font-family: Arial, sans-serif; background-color: #f5f7fa; padding: 30px;'>

    <div style='max-width: 600px; margin: auto; background: white; padding: 30px; border-radius: 10px;'>

        <h2 style='color: #333;'>Password Reset Request</h2>

        <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(user.FullName)}</strong>,</p>

        <p>
            We received a request to reset the password for your account.
        </p>

        <p>
            Click the button below to create a new password:
        </p>

        <p style='text-align: center; margin: 30px 0;'>
            <a href='{System.Net.WebUtility.HtmlEncode(resetLink)}'
               style='background-color: #6c63ff;
                      color: white;
                      padding: 12px 25px;
                      text-decoration: none;
                      border-radius: 6px;
                      display: inline-block;'>
                Reset Password
            </a>
        </p>

        <p>
            This password reset link will expire in <strong>30 minutes</strong>.
        </p>

        <p>
            If you did not request a password reset, you can safely ignore this email.
        </p>

        <hr>

        <p style='font-size: 12px; color: #777;'>
            Housing Allotment Management System
        </p>

    </div>

</body>
</html>";

            // -----------------------------------------------------
            // SEND EMAIL
            // -----------------------------------------------------

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Housing Allotment Management System - Password Reset",
                    emailBody);
            }
            catch (Exception)
            {
                // Remove token if email could not be sent
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                await _context.SaveChangesAsync();

                ModelState.AddModelError(
                    "",
                    "Unable to send the password reset email. Please try again later.");

                return View(model);
            }

            TempData["ForgotPasswordMessage"] =
                "If an account exists with this email address, a password reset link has been sent.";

            return RedirectToAction(
                nameof(ForgotPassword));
        }


        // =========================================================
        // RESET PASSWORD - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ResetPassword(
            string? email,
            string? token)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                TempData["ResetPasswordError"] =
                    "The password reset link is invalid.";

                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            string normalizedEmail =
                email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == normalizedEmail &&
                    u.PasswordResetToken == token);

            if (user == null)
            {
                TempData["ResetPasswordError"] =
                    "The password reset link is invalid or has already been used.";

                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            // -----------------------------------------------------
            // CHECK EXPIRY
            // -----------------------------------------------------

            if (!user.PasswordResetTokenExpiry.HasValue ||
                user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                await _context.SaveChangesAsync();

                TempData["ResetPasswordError"] =
                    "The password reset link has expired. Please request a new one.";

                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Email = user.Email,
                Token = token
            };

            return View(model);
        }


        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email =
                model.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.PasswordResetToken == model.Token);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "The password reset link is invalid or has already been used.");

                return View(model);
            }

            // -----------------------------------------------------
            // CHECK TOKEN EXPIRY
            // -----------------------------------------------------

            if (!user.PasswordResetTokenExpiry.HasValue ||
                user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                await _context.SaveChangesAsync();

                ModelState.AddModelError(
                    "",
                    "The password reset link has expired. Please request a new one.");

                return View(model);
            }

            // -----------------------------------------------------
            // UPDATE PASSWORD
            // -----------------------------------------------------
            //
            // Your current Login/Register system stores the password
            // directly in PasswordHash.
            //
            // We therefore keep the same format here so existing
            // authentication continues to work.
            // -----------------------------------------------------

            user.PasswordHash = model.Password;

            // -----------------------------------------------------
            // INVALIDATE TOKEN
            // -----------------------------------------------------

            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your password has been reset successfully. You can now login with your new password.";

            return RedirectToAction(
                nameof(Login));
        }


        // =========================================================
        // CLIENT RETURN URL VALIDATION
        // =========================================================

        private bool IsClientAllowedReturnUrl(
            string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return false;
            }

            if (!Url.IsLocalUrl(returnUrl))
            {
                return false;
            }

            string path =
                returnUrl.Split('?', '#')[0];

            path =
                "/" +
                path.Trim('/');

            // -----------------------------------------------------
            // HOME
            // -----------------------------------------------------

            if (path.Equals(
                    "/",
                    StringComparison.OrdinalIgnoreCase) ||

                path.Equals(
                    "/Home",
                    StringComparison.OrdinalIgnoreCase) ||

                path.Equals(
                    "/Home/Index",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // -----------------------------------------------------
            // HOUSING SCHEMES
            // -----------------------------------------------------

            if (path.Equals(
                    "/Home/HousingSchemes",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // -----------------------------------------------------
            // SCHEME DETAILS
            // -----------------------------------------------------

            if (path.Equals(
                    "/Home/SchemeDetails",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // -----------------------------------------------------
            // APPLY
            // -----------------------------------------------------

            if (path.Equals(
                    "/Applications/Apply",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // -----------------------------------------------------
            // MY APPLICATIONS
            // -----------------------------------------------------

            if (path.Equals(
                    "/Applications/MyApplications",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // -----------------------------------------------------
            // MY APPLICATION DETAILS
            // -----------------------------------------------------

            if (path.Equals(
                    "/Applications/MyApplicationDetails",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            if (path.Equals(
                    "/Applications/Success",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }


        // =========================================================
        // NORMALIZE PHONE NUMBER
        // =========================================================

        private static string NormalizePhoneNumber(
            string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return string.Empty;
            }

            return new string(
                mobile
                    .Where(char.IsDigit)
                    .ToArray());
        }


        // =========================================================
        // LOGOUT - GET
        // =========================================================

        [HttpGet]
        public IActionResult Logout()
        {
            return View();
        }


        // =========================================================
        // LOGOUT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutConfirmed()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(
                nameof(Login));
        }


        // =========================================================
        // ACCESS DENIED
        // =========================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}