using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    // =========================================================
    // ADMIN ONLY CONTROLLER
    // =========================================================
    //
    // Every action in this controller is restricted to Admin.
    //
    // A normal Client/User cannot access:
    //
    // /Users
    // /Users/Details
    // /Users/Create
    // /Users/Edit
    // /Users/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.UserId)
                .AsNoTracking()
                .ToListAsync();

            return View(users);
        }


        // =========================================================
        // DETAILS - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? userid)
        {
            if (userid == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == userid);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadRoles();

            return View(new User
            {
                IsVerified = false,
                Status = true
            });
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "RoleId,FullName,Email,Mobile,PasswordHash," +
                "Gender,Dob,Address,City,State,Pincode," +
                "AadhaarNumber,Pannumber,Occupation,AnnualIncome," +
                "ProfileImage,IsVerified,Status")]
            User user)
        {
            // Navigation property is not submitted by the form.
            ModelState.Remove("Role");

            // -----------------------------------------------------
            // ROLE VALIDATION
            // -----------------------------------------------------

            if (user.RoleId <= 0)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Please select a role.");
            }

            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (ModelState.IsValid)
            {
                try
                {
                    user.CreatedDate =
                        DateTime.Now;

                    _context.Users.Add(user);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "User created successfully.";

                    return RedirectToAction(
                        nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to save user. " +
                        (ex.InnerException?.Message ??
                         ex.Message));
                }
            }

            // Reload roles after validation/database error.
            LoadRoles(user.RoleId);

            return View(user);
        }


        // =========================================================
        // EDIT - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? userid)
        {
            if (userid == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == userid);

            if (user == null)
            {
                return NotFound();
            }

            LoadRoles(user.RoleId);

            return View(user);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int userid,
            [Bind(
                "UserId,RoleId,FullName,Email,Mobile,PasswordHash," +
                "Gender,Dob,Address,City,State,Pincode," +
                "AadhaarNumber,Pannumber,Occupation,AnnualIncome," +
                "ProfileImage,IsVerified,Status,CreatedDate")]
            User user)
        {
            // -----------------------------------------------------
            // ID VALIDATION
            // -----------------------------------------------------

            if (userid != user.UserId)
            {
                return NotFound();
            }

            // Navigation property is not submitted by the form.
            ModelState.Remove("Role");

            // -----------------------------------------------------
            // ROLE VALIDATION
            // -----------------------------------------------------

            if (user.RoleId <= 0)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Please select a role.");
            }

            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                LoadRoles(user.RoleId);

                return View(user);
            }

            // -----------------------------------------------------
            // UPDATE USER
            // -----------------------------------------------------

            try
            {
                var existingUser =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.UserId == userid);

                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.RoleId =
                    user.RoleId;

                existingUser.FullName =
                    user.FullName;

                existingUser.Email =
                    user.Email;

                existingUser.Mobile =
                    user.Mobile;

                existingUser.PasswordHash =
                    user.PasswordHash;

                existingUser.Gender =
                    user.Gender;

                // Model property is Dob.
                existingUser.Dob =
                    user.Dob;

                existingUser.Address =
                    user.Address;

                existingUser.City =
                    user.City;

                existingUser.State =
                    user.State;

                existingUser.Pincode =
                    user.Pincode;

                existingUser.AadhaarNumber =
                    user.AadhaarNumber;

                // Model property is Pannumber.
                existingUser.Pannumber =
                    user.Pannumber;

                existingUser.Occupation =
                    user.Occupation;

                existingUser.AnnualIncome =
                    user.AnnualIncome;

                existingUser.ProfileImage =
                    user.ProfileImage;

                existingUser.IsVerified =
                    user.IsVerified;

                existingUser.Status =
                    user.Status;

                // CreatedDate is intentionally preserved.

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "User updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to update user. " +
                    (ex.InnerException?.Message ??
                     ex.Message));

                LoadRoles(user.RoleId);

                return View(user);
            }
        }


        // =========================================================
        // DELETE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? userid)
        {
            if (userid == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == userid);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        // =========================================================
        // DELETE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int userid)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == userid);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "User deleted successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This user cannot be deleted because the user is linked with another record.";

                return RedirectToAction(
                    nameof(Delete),
                    new
                    {
                        userid
                    });
            }
        }


        // =========================================================
        // LOAD ROLES
        // =========================================================

        private void LoadRoles(
            int? selectedRoleId = null)
        {
            ViewBag.RoleId =
                new SelectList(
                    _context.Roles
                        .AsNoTracking()
                        .OrderBy(r => r.RoleName)
                        .ToList(),
                    "RoleId",
                    "RoleName",
                    selectedRoleId);
        }


        // =========================================================
        // CHECK USER EXISTS
        // =========================================================

        private bool UserExists(int userid)
        {
            return _context.Users
                .Any(u => u.UserId == userid);
        }
    }
}