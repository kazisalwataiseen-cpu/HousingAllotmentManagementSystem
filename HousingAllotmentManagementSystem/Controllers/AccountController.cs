using System.Security.Claims;
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
            //
            // Your current registration system stores the password
            // directly in PasswordHash, so this comparison matches
            // your current database structure.
            //
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
                // Admin can use a valid local return URL
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

            // Client can only go to allowed client pages
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
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            // -----------------------------------------------------
            // MODEL VALIDATION
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // -----------------------------------------------------
            // NORMALIZE VALUES
            // -----------------------------------------------------

            string fullName =
                model.FullName?.Trim() ?? string.Empty;

            string email =
                model.Email?.Trim() ?? string.Empty;

            string mobile =
                NormalizePhoneNumber(model.Mobile);

            string password =
                model.Password ?? string.Empty;

            // -----------------------------------------------------
            // FULL NAME
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError(
                    "FullName",
                    "Please enter your full name.");

                return View(model);
            }

            // -----------------------------------------------------
            // EMAIL
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Please enter your email address.");

                return View(model);
            }

            var emailValidator =
                new System.ComponentModel.DataAnnotations
                    .EmailAddressAttribute();

            if (!emailValidator.IsValid(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Please enter a valid email address.");

                return View(model);
            }

            // -----------------------------------------------------
            // MOBILE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(mobile))
            {
                ModelState.AddModelError(
                    "Mobile",
                    "Please enter your phone number.");

                return View(model);
            }

            if (mobile.Length != 10 ||
                !mobile.All(char.IsDigit))
            {
                ModelState.AddModelError(
                    "Mobile",
                    "Please enter a valid 10-digit phone number.");

                return View(model);
            }

            // -----------------------------------------------------
            // PASSWORD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Please enter a password.");

                return View(model);
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError(
                    "Password",
                    "Password must contain at least 6 characters.");

                return View(model);
            }

            // -----------------------------------------------------
            // CHECK EMAIL
            // -----------------------------------------------------

            bool emailExists =
                await _context.Users
                    .AnyAsync(u =>
                        u.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email address already exists.");

                return View(model);
            }

            // -----------------------------------------------------
            // CHECK MOBILE
            // -----------------------------------------------------

            bool mobileExists =
                await _context.Users
                    .AnyAsync(u =>
                        u.Mobile == mobile);

            if (mobileExists)
            {
                ModelState.AddModelError(
                    "Mobile",
                    "An account with this phone number already exists.");

                return View(model);
            }

            // =====================================================
            // GET CLIENT ROLE
            // =====================================================

            var clientRole =
                await _context.Roles
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

                FullName = fullName,

                Email = email,

                Mobile = mobile,

                // Matches the existing login implementation.
                // Password hashing can be added later.
                PasswordHash = password,

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

                return RedirectToAction(
                    nameof(Login));
            }
            catch (DbUpdateException ex)
            {
                string errorMessage =
                    ex.InnerException?.Message ??
                    ex.Message;

                ModelState.AddModelError(
                    "",
                    "Unable to create your account. " +
                    errorMessage);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred while creating your account. " +
                    ex.Message);

                return View(model);
            }
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